using System.Text.RegularExpressions;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using SR.Core;
using Match = SR.Core.Match;

namespace SR.Scraper;

/// <summary>Scraping and page parsing: organizer tournaments -> matches -> standings.
/// Port of scraper.py to AngleSharp.</summary>
public sealed partial class Scraper
{
    private readonly AppConfig _cfg;
    private readonly TournamentClient _client;
    private readonly HtmlParser _parser = new();

    public Scraper(AppConfig cfg, TournamentClient client)
    {
        _cfg = cfg;
        _client = client;
    }

    private async Task<IDocument> LoadAsync(string url, bool xhr = false) =>
        await _parser.ParseDocumentAsync(await _client.FetchAsync(url, xhr));

    /// <summary>All organizer tournaments, filtered by the league name.</summary>
    public async Task<List<Tournament>> DiscoverTournamentsAsync()
    {
        var url = $"{_cfg.BaseUrl}/find.aspx?a=7&q={_cfg.OrganizerId}";
        var doc = await LoadAsync(url);
        var pattern = new Regex(_cfg.TournamentNamePattern, RegexOptions.IgnoreCase);
        var tournaments = new List<Tournament>();

        foreach (var a in doc.QuerySelectorAll("a[href*=\"tournament.aspx?id=\"]"))
        {
            var href = a.GetAttribute("href") ?? "";
            var idMatch = GuidRe().Match(href);
            if (!idMatch.Success) continue;
            var guid = idMatch.Groups[1].Value.ToLowerInvariant();
            var name = a.TextContent.Trim();
            if (name.Length == 0 || !pattern.IsMatch(name)) continue;
            if (tournaments.Any(t => t.Id == guid)) continue;

            var dm = DateRe().Match(name);
            var date = dm.Success ? $"{dm.Groups[3].Value}-{dm.Groups[2].Value}-{dm.Groups[1].Value}" : null;
            tournaments.Add(new Tournament { Id = guid, Name = name, Date = date });
        }

        // One-off tournaments from other organizers, named explicitly in config
        // (their titles don't carry a date and don't match TournamentNamePattern).
        foreach (var extra in _cfg.ExtraTournaments)
        {
            var guid = extra.Id.ToLowerInvariant();
            if (tournaments.Any(t => t.Id == guid)) continue;
            tournaments.Add(new Tournament { Id = guid, Name = extra.Name, Date = extra.Date });
        }

        return tournaments.OrderBy(t => t.Date ?? "", StringComparer.Ordinal).ToList();
    }

    /// <summary>Matches page -> list of matches (deduplicating the list/grid views).
    /// Multi-day tournaments split their matches across day tabs; the default page
    /// only renders the selected day, so every day is fetched and merged.</summary>
    public async Task<List<Match>> ParseMatchesAsync(Tournament tournament)
    {
        var matchesUrl = $"{_cfg.BaseUrl}/tournament/{tournament.Id}/Matches";
        var doc = await LoadAsync(matchesUrl);

        var days = doc.QuerySelectorAll("a.js-date-selection-tab")
                      .Select(a => a.GetAttribute("data-value"))
                      .Where(d => !string.IsNullOrEmpty(d))
                      .Distinct()
                      .ToList();

        var matches = new List<Match>();
        var seq = 0;
        if (days.Count == 0)
        {
            // Single-day tournament — the default page already holds every match.
            ParseMatchGroups(doc, matches, ref seq);
        }
        else
        {
            // Multi-day — fetch each day's fragment (the default page shows only one).
            foreach (var day in days)
            {
                var dayDoc = await LoadAsync($"{matchesUrl}/MatchesInDay?date={day}", xhr: true);
                ParseMatchGroups(dayDoc, matches, ref seq);
            }
        }

        return Deduplicate(matches);
    }

    /// <summary>Parses every match-group in a matches document (whole page or a day
    /// fragment) into <paramref name="matches"/>, continuing the shared sequence.</summary>
    private void ParseMatchGroups(IDocument doc, List<Match> matches, ref int seq)
    {
        foreach (var ol in doc.QuerySelectorAll("ol.match-group"))
        {
            var header = ol.PreviousElementSibling;
            while (header != null && !header.TagName.Equals("H5", StringComparison.OrdinalIgnoreCase))
                header = header.PreviousElementSibling;
            var timeLabel = header?.TextContent.Trim();

            foreach (var div in ol.QuerySelectorAll("div.match--list"))
            {
                seq++;
                var drawA = div.QuerySelector(".match__header-title-item a");
                if (drawA == null) continue;
                var drawName = drawA.TextContent.Trim();
                int? drawId = null;
                var dm = DrawRe().Match(drawA.GetAttribute("href") ?? "");
                if (dm.Success) drawId = int.Parse(dm.Groups[1].Value);

                var titleItems = div.QuerySelectorAll(".match__header-title-item");
                var round = titleItems.Length > 1 ? titleItems[1].TextContent.Trim() : null;

                var sides = new List<Side>();
                foreach (var row in div.QuerySelectorAll(".match__body .match__row"))
                {
                    var players = row.QuerySelectorAll("a[data-player-id]")
                                     .Select(a => a.TextContent.Trim()).ToList();
                    string? placeholder = null;
                    if (players.Count == 0)
                        placeholder = row.QuerySelector(".match__row-title-value-content")?.TextContent.Trim();
                    sides.Add(new Side
                    {
                        Players = players,
                        Placeholder = placeholder,
                        Won = row.ClassList.Contains("has-won"),
                    });
                }
                if (sides.Count != 2) continue;

                var games = new List<int[]>();
                foreach (var ul in div.QuerySelectorAll(".match__result ul.points"))
                {
                    var cells = ul.QuerySelectorAll("li.points__cell")
                                  .Select(c => c.TextContent.Trim()).ToList();
                    if (cells.Count == 2 && cells.All(IsDigits))
                        games.Add(new[] { int.Parse(cells[0]), int.Parse(cells[1]) });
                }

                var (evt, group) = Events.ParseEventCode(drawName);
                var (discipline, level) = Events.EventMeta(evt);
                var isBye = sides.Any(s => (s.Placeholder ?? "").Trim().Equals("bye", StringComparison.OrdinalIgnoreCase));

                matches.Add(new Match
                {
                    Bye = isBye,
                    Seq = seq,
                    Time = timeLabel,
                    DrawId = drawId,
                    DrawName = drawName,
                    Event = evt,
                    Group = group,
                    Discipline = discipline,
                    Level = level,
                    Round = round,
                    Sides = sides,
                    Games = games,
                    Walkover = games.Count == 0,
                });
            }
        }
    }

    private static List<Match> Deduplicate(List<Match> matches)
    {
        var seen = new HashSet<string>();
        var unique = new List<Match>();
        foreach (var m in matches)
        {
            var sidesSig = string.Join("::", m.Sides.Select(s =>
                s.Players.Count > 0 ? string.Join(",", s.Players) : s.Placeholder));
            var gamesSig = string.Join(";", m.Games.Select(g => $"{g[0]}-{g[1]}"));
            var key = $"{m.DrawName}|{m.Round}|{sidesSig}|{gamesSig}";
            if (seen.Add(key)) unique.Add(m);
        }
        return unique;
    }

    /// <summary>GetStandings -> group positions (position -> players).</summary>
    public async Task<List<Standing>> ParseStandingsAsync(Tournament tournament, int drawId)
    {
        var url = $"{_cfg.BaseUrl}/tournament/{tournament.Id}/Draw/{drawId}/GetStandings";
        var doc = await LoadAsync(url, xhr: true);
        var rows = new List<Standing>();

        foreach (var tr in doc.QuerySelectorAll("table tr"))
        {
            var tds = tr.QuerySelectorAll("td");
            if (tds.Length == 0) continue;
            var posText = tds[0].TextContent.Trim();
            if (!IsDigits(posText)) continue;

            var players = tr.QuerySelectorAll("a").Select(a => a.TextContent.Trim())
                            .Where(s => s.Length > 0).ToList();
            if (players.Count == 0)
            {
                var name = tds[1].TextContent.Trim();
                if (name.Length > 0) players.Add(name);
            }
            if (players.Count > 0)
                rows.Add(new Standing { Pos = int.Parse(posText), Players = players });
        }
        return rows;
    }

    /// <summary>"Group A #2" in the playoff -> the players ranked 2nd in "SE - Group A".</summary>
    public static int ResolvePlaceholders(List<Match> matches, Dictionary<string, List<Standing>> standingsByDrawName)
    {
        var unresolved = 0;
        foreach (var m in matches)
        {
            foreach (var side in m.Sides)
            {
                if (side.Players.Count > 0 || string.IsNullOrEmpty(side.Placeholder)) continue;
                var g = PlaceholderRe().Match(side.Placeholder);
                if (!g.Success) { unresolved++; continue; }

                var drawName = $"{m.Event} - {g.Groups[1].Value}";
                var pos = int.Parse(g.Groups[2].Value);
                if (standingsByDrawName.TryGetValue(drawName, out var standing))
                {
                    var row = standing.FirstOrDefault(r => r.Pos == pos);
                    if (row != null)
                    {
                        side.Players = new List<string>(row.Players);
                        side.ResolvedFrom = side.Placeholder;
                        continue;
                    }
                }
                unresolved++;
            }
        }
        return unresolved;
    }

    /// <summary>Collapses whitespace and applies the player alias map.</summary>
    public void NormalizeNames(List<Match> matches)
    {
        foreach (var m in matches)
            foreach (var side in m.Sides)
                side.Players = side.Players.Select(p =>
                {
                    var clean = WhitespaceRe().Replace(p, " ").Trim();
                    return _cfg.PlayerAliases.GetValueOrDefault(clean, clean);
                }).ToList();
    }

    private static bool IsDigits(string s) => s.Length > 0 && s.All(char.IsDigit);

    [GeneratedRegex(@"id=([A-Fa-f0-9-]{36})")] private static partial Regex GuidRe();
    [GeneratedRegex(@"(\d{2})\.(\d{2})\.(\d{4})")] private static partial Regex DateRe();
    [GeneratedRegex(@"draw=(\d+)")] private static partial Regex DrawRe();
    [GeneratedRegex(@"^(Group .+?) #(\d+)$")] private static partial Regex PlaceholderRe();
    [GeneratedRegex(@"\s+")] private static partial Regex WhitespaceRe();
}
