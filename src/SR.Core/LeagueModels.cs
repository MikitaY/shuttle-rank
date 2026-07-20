namespace SR.Core;

/// <summary>Intermediate model (league.json): tournaments, matches, sides.</summary>
public sealed class LeagueData
{
    public List<Tournament> Tournaments { get; set; } = new();
}

public sealed class Tournament
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Date { get; set; }
    public List<Match> Matches { get; set; } = new();
}

public sealed class Match
{
    public bool Bye { get; set; }
    public int Seq { get; set; }
    public string? Time { get; set; }
    public int? DrawId { get; set; }
    public string DrawName { get; set; } = "";
    public string Event { get; set; } = "";
    public string? Group { get; set; }
    public string Discipline { get; set; } = "";
    public string? Level { get; set; }
    public string? Round { get; set; }
    public List<Side> Sides { get; set; } = new();
    public List<int[]> Games { get; set; } = new();
    public bool Walkover { get; set; }
}

public sealed class Side
{
    public List<string> Players { get; set; } = new();
    public string? Placeholder { get; set; }
    public bool Won { get; set; }
    public string? ResolvedFrom { get; set; }
}

/// <summary>A group standings row: position -> players.</summary>
public sealed class Standing
{
    public int Pos { get; set; }
    public List<string> Players { get; set; } = new();
}
