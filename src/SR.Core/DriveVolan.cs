using System.Text.RegularExpressions;

namespace SR.Core;

/// <summary>A drivevolan.by tournament as tools/drivevolan/export.js saves it
/// (data/drivevolan/tournaments/&lt;id&gt;.json): names and rounds exactly as the site shows them.</summary>
public sealed class DriveVolanExport
{
    public int Id { get; set; }
    public string Url { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Date { get; set; }
    public List<DriveVolanPlayer> Players { get; set; } = new();
    public List<DriveVolanMatch> Matches { get; set; } = new();
}

public sealed class DriveVolanPlayer
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
}

public sealed class DriveVolanMatch
{
    /// <summary>Same codes as tournamentsoftware events: SB, DC, WDD, XDE…</summary>
    public string Category { get; set; } = "";
    /// <summary>"Группа A", "Полуфинал", "Матч за 3 место"… or null for a round-robin draw.</summary>
    public string? Round { get; set; }
    public string? Court { get; set; }
    public string? Time { get; set; }
    public List<string> SideA { get; set; } = new();
    public List<string> SideB { get; set; } = new();
    /// <summary>"a" or "b".</summary>
    public string? Winner { get; set; }
    /// <summary>"15-12, 15-8", from side A's point of view.</summary>
    public string Score { get; set; } = "";
    public string Status { get; set; } = "";
}

/// <summary>Converts drivevolan.by exports into the league model, so they rate exactly like
/// tournamentsoftware events. Player names go through the reviewed drivevolan → ours map
/// (data/drivevolan/players.csv); the config alias map is applied afterwards, as for every source.</summary>
public static partial class DriveVolan
{
    public const string IdPrefix = "drivevolan-";

    public static Tournament ToTournament(DriveVolanExport export, IReadOnlyDictionary<string, string> names)
    {
        var unmapped = export.Matches.SelectMany(m => m.SideA.Concat(m.SideB))
                                     .Where(n => !names.ContainsKey(n))
                                     .Distinct()
                                     .ToList();
        if (unmapped.Count > 0)
            throw new InvalidOperationException(
                $"drivevolan tournament {export.Id}: players missing from players.csv: {string.Join(", ", unmapped)}");

        var tournament = new Tournament
        {
            Id = IdPrefix + export.Id,
            Name = export.Name,
            Date = export.Date,
        };

        var seq = 0;
        foreach (var dv in export.Matches)
        {
            seq++;
            var (group, round) = ParseRound(dv.Round);
            var (discipline, level) = Events.EventMeta(dv.Category);
            var games = ParseScore(dv.Score);

            tournament.Matches.Add(new Match
            {
                Seq = seq,
                Time = dv.Time,
                DrawName = group != null ? $"{dv.Category} - {group}" : dv.Category,
                Event = dv.Category,
                Group = group,
                Discipline = discipline,
                Level = level,
                Round = round,
                Sides =
                [
                    new Side { Players = dv.SideA.Select(n => names[n]).ToList(), Won = dv.Winner == "a" },
                    new Side { Players = dv.SideB.Select(n => names[n]).ToList(), Won = dv.Winner == "b" },
                ],
                Games = games,
                Walkover = IsWalkover(games),
            });
        }
        return tournament;
    }

    /// <summary>The site's round label -> (group, round) in tournamentsoftware's terms, so that
    /// <see cref="Events.RoundKind"/> awards the same stage points: "Группа A" -> ("Group A", null),
    /// "Полуфинал" -> (null, "Semi final"), "Матч а 5-8 место" -> (null, "Places 5-8").</summary>
    public static (string? Group, string? Round) ParseRound(string? label)
    {
        var r = (label ?? "").Trim();
        if (r.Length == 0) return (null, null);
        var group = GroupRe().Match(r);
        if (group.Success) return ($"Group {group.Groups[1].Value}", null);
        if (r == "Финал") return (null, "Final");
        if (r == "Полуфинал") return (null, "Semi final");
        if (r == "Четвертьфинал") return (null, "Quarter final");
        if (r == "Матч за 3 место") return (null, "3rd/4th place");
        var places = PlacesRe().Match(r);
        if (places.Success) return (null, $"Places {places.Groups[1].Value}");
        return (null, r);
    }

    /// <summary>"15-12, 15-8" -> [[15, 12], [15, 8]].</summary>
    public static List<int[]> ParseScore(string score) =>
        ScoreRe().Matches(score)
                 .Select(m => new[] { int.Parse(m.Groups[1].Value), int.Parse(m.Groups[2].Value) })
                 .ToList();

    /// <summary>The site records a no-show as a straight-zero result (21-0, 21-0); like a
    /// tournamentsoftware walkover it counts for the match tally but not for Elo.</summary>
    public static bool IsWalkover(List<int[]> games) =>
        games.Count > 0 && games.All(g => g[0] == 0 || g[1] == 0);

    [GeneratedRegex(@"^Группа\s+(\S+)$")] private static partial Regex GroupRe();
    [GeneratedRegex(@"(\d+(?:-\d+)?)\s+место$")] private static partial Regex PlacesRe();
    [GeneratedRegex(@"(\d+)\s*-\s*(\d+)")] private static partial Regex ScoreRe();
}
