using System.Text.Json;
using SR.Core;

namespace SR.Scraper;

/// <summary>Reads the committed drivevolan.by exports. The site only shows results to a
/// signed-in user, so they are exported by hand (tools/drivevolan/export.js) instead of being
/// scraped on every run.</summary>
public static class DriveVolanSource
{
    public static List<Tournament> Load(string dir)
    {
        var tournamentsDir = Path.Combine(dir, "tournaments");
        if (!Directory.Exists(tournamentsDir)) return new();

        var names = LoadNames(Path.Combine(dir, "players.csv"));
        return Directory.GetFiles(tournamentsDir, "*.json")
            .Select(f => JsonSerializer.Deserialize<DriveVolanExport>(File.ReadAllText(f), Json.Options)
                         ?? throw new InvalidOperationException($"Could not read {f}"))
            .Select(e => DriveVolan.ToTournament(e, names))
            .ToList();
    }

    /// <summary>players.csv: name as drivevolan shows it -> our player. Keyed by name, not by
    /// drivevolan id: match rows carry only names, and two profiles of one person share a name.</summary>
    private static Dictionary<string, string> LoadNames(string path)
    {
        var names = new Dictionary<string, string>();
        foreach (var line in File.ReadLines(path).Skip(1))
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var cols = SplitCsv(line);
            names[cols[0]] = cols[1];
        }
        return names;
    }

    /// <summary>Semicolon-separated, with Excel-style quoting for cells that contain ';' or '"'.</summary>
    private static List<string> SplitCsv(string line)
    {
        var cols = new List<string>();
        var cell = new System.Text.StringBuilder();
        var quoted = false;
        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];
            if (quoted)
            {
                if (c == '"' && i + 1 < line.Length && line[i + 1] == '"') { cell.Append('"'); i++; }
                else if (c == '"') quoted = false;
                else cell.Append(c);
            }
            else if (c == '"') quoted = true;
            else if (c == ';') { cols.Add(cell.ToString()); cell.Clear(); }
            else cell.Append(c);
        }
        cols.Add(cell.ToString());
        return cols;
    }
}
