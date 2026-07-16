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
    public Dictionary<string, string> PlayerAliases { get; set; } = new();
    public EloConfig Elo { get; set; } = new();
    public PointsConfig Points { get; set; } = new();
    public LevelInferenceConfig LevelInference { get; set; } = new();

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
