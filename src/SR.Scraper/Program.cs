using System.Text;
using System.Text.Json;
using SR.Core;
using SR.Scraper;

Console.OutputEncoding = Encoding.UTF8;

var refresh = args.Contains("--refresh");
var root = FindRoot();
var cfg = AppConfig.Load(Path.Combine(root, "config.json"));

using var client = new TournamentClient(cfg, Path.Combine(root, "data", "cache"), refresh);
var scraper = new Scraper(cfg, client);

var tournaments = await scraper.DiscoverTournamentsAsync();
Console.WriteLine($"Турниров найдено: {tournaments.Count}");

var league = new LeagueData();
foreach (var t in tournaments)
{
    Console.WriteLine($"  {t.Date}  {t.Name}");
    var matches = await scraper.ParseMatchesAsync(t);

    // Групповые draw'ы (drawName -> drawId) для получения standings.
    var groupDraws = new Dictionary<string, int>();
    foreach (var m in matches)
        if (!string.IsNullOrEmpty(m.Group) && m.DrawId is int id)
            groupDraws[m.DrawName] = id;

    var standings = new Dictionary<string, List<Standing>>();
    foreach (var (drawName, drawId) in groupDraws)
        standings[drawName] = await scraper.ParseStandingsAsync(t, drawId);

    var unresolved = Scraper.ResolvePlaceholders(matches, standings);
    scraper.NormalizeNames(matches);

    var played = matches.Count(m => m.Sides.Any(s => s.Won));
    Console.WriteLine($"    матчей: {matches.Count}, сыграно: {played}, нераскрытых сторон: {unresolved}");

    t.Matches = matches;
    league.Tournaments.Add(t);
}

// Промежуточный league.json (для отладки; в репозиторий не коммитится).
var leaguePath = Path.Combine(root, "data", "league.json");
Directory.CreateDirectory(Path.GetDirectoryName(leaguePath)!);
await WriteJsonAsync(leaguePath, league);
Console.WriteLine($"Записано: {leaguePath}");

// Рейтинги -> в данные фронтенда.
var ratings = new RatingEngine(cfg).Compute(league);
ratings.Updated = DateTime.UtcNow.ToString("yyyy-MM-dd");
var ratingsPath = Path.Combine(root, "web", "public", "data", "ratings.json");
Directory.CreateDirectory(Path.GetDirectoryName(ratingsPath)!);
await WriteJsonAsync(ratingsPath, ratings);
Console.WriteLine($"Игроков: {ratings.Players.Count}. Записано: {ratingsPath}");

Console.WriteLine("\nТоп-10 по Elo (общий):");
for (var i = 0; i < Math.Min(10, ratings.Players.Count); i++)
{
    var p = ratings.Players[i];
    Console.WriteLine($"{i + 1,3}. {p.Name,-30} {p.Elo["overall"],7:F1}  "
        + $"lvl {p.LevelLabel ?? "-",-7}  {p.Wins}-{p.Losses}  очки {p.Points:F0}");
}

return;

static async Task WriteJsonAsync<T>(string path, T value)
{
    var json = JsonSerializer.Serialize(value, Json.Options);
    await File.WriteAllTextAsync(path, json, new UTF8Encoding(false));
}

// Ищет корень репозитория — каталог, содержащий config.json.
static string FindRoot()
{
    foreach (var start in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
    {
        var dir = new DirectoryInfo(start);
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "config.json"))) return dir.FullName;
            dir = dir.Parent;
        }
    }
    throw new InvalidOperationException("Не найден config.json в корне репозитория.");
}
