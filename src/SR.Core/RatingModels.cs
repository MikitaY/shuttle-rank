namespace SR.Core;

/// <summary>Final model (ratings.json) consumed by the frontend.</summary>
public sealed class RatingsOutput
{
    public List<TournamentSummary> Tournaments { get; set; } = [];
    public List<PlayerOut> Players { get; set; } = [];
    public string? Updated { get; set; }
}

public sealed class TournamentSummary
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Date { get; set; }
    public int Matches { get; set; }
}

public sealed class PlayerOut
{
    public string Name { get; set; } = "";
    public string? Level { get; set; }
    public string? LevelLabel { get; set; }
    public string? LevelTrend { get; set; }
    public Dictionary<string, int> LevelsPlayed { get; set; } = new();
    public Dictionary<string, double> Elo { get; set; } = new();
    public Dictionary<string, int> EloMatches { get; set; } = new();
    public double Points { get; set; }
    public Dictionary<string, double> PointsByDiscipline { get; set; } = new();
    public Dictionary<string, double> PointsByLevel { get; set; } = new();
    public int Tournaments { get; set; }
    public int Matches { get; set; }
    public int Wins { get; set; }
    public int Losses { get; set; }
    public double Winrate { get; set; }
    public int Walkovers { get; set; }
    public Dictionary<string, DisciplineStat> ByDiscipline { get; set; } = new();
    public Dictionary<string, DisciplineStat> ByLevel { get; set; } = new();
    public List<string> Form { get; set; } = new();
    public List<MatchLogEntry> MatchLog { get; set; } = new();
}

public sealed class DisciplineStat
{
    public int Matches { get; set; }
    public int Wins { get; set; }
    public int Losses { get; set; }
    public double Winrate { get; set; }
    public List<string> Form { get; set; } = new();
}

public sealed class MatchLogEntry
{
    public string? Date { get; set; }
    public string Event { get; set; } = "";
    public string Discipline { get; set; } = "";
    public string? Round { get; set; }
    public bool Won { get; set; }
    public bool Walkover { get; set; }
    public List<string> Teammates { get; set; } = new();
    public List<string> Opponents { get; set; } = new();
    public List<int[]> Games { get; set; } = new();
}
