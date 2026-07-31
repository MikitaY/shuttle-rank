using System.Text.Json;

namespace SR.Core;

/// <summary>config.json: data-source settings and rating algorithm parameters.</summary>
public sealed class AppConfig
{
    public string OrganizerId { get; set; } = "";
    public string TournamentNamePattern { get; set; } = "";
    public string BaseUrl { get; set; } = "";
    public string Cookie { get; set; } = "";
    public string UserAgent { get; set; } = "";
    public List<ExtraTournamentConfig> ExtraTournaments { get; set; } = new();
    public Dictionary<string, string> PlayerAliases { get; set; } = new();
    public EloConfig Elo { get; set; } = new();
    public PointsConfig Points { get; set; } = new();
    public LevelInferenceConfig LevelInference { get; set; } = new();
    public UnifiedConfig Unified { get; set; } = new();

    public static AppConfig Load(string path)
    {
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<AppConfig>(json, Json.Options)
               ?? throw new InvalidOperationException($"Could not read {path}");
    }
}

public sealed class EloConfig
{
    public double Initial { get; set; } = 1500;
    public double KFactor { get; set; } = 32;
    public double Scale { get; set; } = 400;
}

public sealed class PointsConfig
{
    public double Participation { get; set; } = 5;
    public Dictionary<string, double> WinByRound { get; set; } = new();
    public Dictionary<string, double> LevelMultiplier { get; set; } = new();
}

public sealed class LevelInferenceConfig
{
    public double WinrateUp { get; set; } = 0.65;
    public double WinrateDown { get; set; } = 0.35;
    public double Adjustment { get; set; } = 0.4;
}

/// <summary>Parameters of the single cross-category ("сквозной") rating: one number per
/// player over every discipline and level, plus the category ladder built on top of it.</summary>
public sealed class UnifiedConfig
{
    /// <summary>Categories from weakest to strongest. Masters is deliberately absent —
    /// it's an age bracket, not a rung on the skill ladder.</summary>
    public List<string> Ladder { get; set; } = new() { "E", "D", "C", "B", "A" };

    /// <summary>Lowest rating that belongs to a category (chess-class bands, 200 wide).</summary>
    public Dictionary<string, double> CategoryFloors { get; set; } =
        new() { ["E"] = 0, ["D"] = 1200, ["C"] = 1400, ["B"] = 1600, ["A"] = 1800 };

    /// <summary>Width of a category band — only used to draw progress inside the open-ended
    /// bottom category.</summary>
    public double BandWidth { get; set; } = 200;

    /// <summary>Starting rating by the level a player entered the league in (band midpoints).
    /// Masters seeds like B — it's the bracket the league's veterans actually compete in.</summary>
    public Dictionary<string, double> SeedByLevel { get; set; } =
        new() { ["E"] = 1100, ["D"] = 1300, ["C"] = 1500, ["B"] = 1700, ["A"] = 1900, ["M"] = 1700 };

    /// <summary>Seed for a player whose first draw carries no level.</summary>
    public double DefaultSeed { get; set; } = 1300;

    public double Scale { get; set; } = 400;

    /// <summary>K-factor by experience: fast while the rating is still finding its place,
    /// slower once it has settled (FIDE-style).</summary>
    public double KProvisional { get; set; } = 40;
    public double KDeveloping { get; set; } = 32;
    public double KSettled { get; set; } = 24;
    public int ProvisionalMatches { get; set; } = 10;
    public int DevelopingMatches { get; set; } = 30;

    /// <summary>Doubles and mixed results move the rating less — half of the outcome
    /// belongs to the partner.</summary>
    public double DoublesWeight { get; set; } = 0.75;

    /// <summary>How far below its own floor a rating must fall before the category drops,
    /// so players don't bounce between two categories.</summary>
    public double DemotionBuffer { get; set; } = 50;

    /// <summary>Distance to the next floor at which a player is shown as transitioning
    /// ("D → C").</summary>
    public double TransitionZone { get; set; } = 60;
}

/// <summary>A tournament outside OrganizerId/TournamentNamePattern, included by id
/// (e.g. a one-off amateur event run by a different organizer).</summary>
public sealed class ExtraTournamentConfig
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Date { get; set; }
}
