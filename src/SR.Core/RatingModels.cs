namespace SR.Core;

/// <summary>Final model (ratings.json) consumed by the frontend.</summary>
public sealed class RatingsOutput
{
    public List<TournamentSummary> Tournaments { get; set; } = [];
    public List<PlayerOut> Players { get; set; } = [];
    public UnifiedParams? UnifiedParams { get; set; }
    public string? Updated { get; set; }
}

/// <summary>Thresholds of the cross-category ladder, so the frontend can describe them
/// without repeating the numbers from config.json.</summary>
public sealed class UnifiedParams
{
    public int ProvisionalMatches { get; set; }
    public double DemotionBuffer { get; set; }
    public double TransitionZone { get; set; }
    public List<UnifiedCategory> Categories { get; set; } = new();
}

public sealed class UnifiedCategory
{
    public string Key { get; set; } = "";
    public double Floor { get; set; }
    public double Seed { get; set; }
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
    public Dictionary<string, double> PointsByCombo { get; set; } = new();
    public int Tournaments { get; set; }
    public int Matches { get; set; }
    public int Wins { get; set; }
    public int Losses { get; set; }
    public double Winrate { get; set; }
    public int Walkovers { get; set; }
    public Dictionary<string, DisciplineStat> ByDiscipline { get; set; } = new();
    public Dictionary<string, DisciplineStat> ByLevel { get; set; } = new();
    public Dictionary<string, DisciplineStat> ByCombo { get; set; } = new();
    public List<string> Form { get; set; } = new();
    public UnifiedOut? Unified { get; set; }
    public List<MatchLogEntry> MatchLog { get; set; } = new();
}

/// <summary>The player's place in the single cross-category rating.</summary>
public sealed class UnifiedOut
{
    public double Rating { get; set; }
    /// <summary>Starting rating, from the category the player entered the league in.</summary>
    public double Seed { get; set; }
    public string? SeedLevel { get; set; }
    /// <summary>Rated matches — walkovers don't count.</summary>
    public int Matches { get; set; }
    public string Category { get; set; } = "";
    public string? NextCategory { get; set; }
    public double Floor { get; set; }
    public double? NextFloor { get; set; }
    /// <summary>0…1 through the current category band.</summary>
    public double Progress { get; set; }
    /// <summary>provisional (too few matches) | promotion (close to the next floor) | stable.</summary>
    public string Status { get; set; } = "";
    public bool Provisional { get; set; }
    public double Peak { get; set; }
    /// <summary>Date the current category was reached; null if never changed from the seed band.</summary>
    public string? CategorySince { get; set; }
    /// <summary>Rating after each tournament the player took part in.</summary>
    public List<UnifiedPoint> History { get; set; } = new();
}

public sealed class UnifiedPoint
{
    public string? Date { get; set; }
    public double Rating { get; set; }
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
    public string? Level { get; set; }
    public string? Round { get; set; }
    public bool Won { get; set; }
    public bool Walkover { get; set; }
    public List<string> Teammates { get; set; } = new();
    public List<string> Opponents { get; set; } = new();
    public List<int[]> Games { get; set; } = new();
    /// <summary>Change of the cross-category rating from this match; null for walkovers.</summary>
    public double? UnifiedDelta { get; set; }
    /// <summary>Cross-category rating right after this match; null for walkovers.</summary>
    public double? UnifiedAfter { get; set; }
}
