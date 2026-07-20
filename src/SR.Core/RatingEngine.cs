namespace SR.Core;

/// <summary>Computes Elo, points and inferred player level from LeagueData.
/// Port of ratings.py — preserves traversal order and formulas.</summary>
public sealed class RatingEngine
{
    private static readonly string[] Levels = { "A", "B", "C", "D", "E", "M" };
    private static readonly Dictionary<string, int> LevelNum =
        new() { ["E"] = 1, ["D"] = 2, ["C"] = 3, ["B"] = 4, ["A"] = 5, ["M"] = 6 };
    private static readonly Dictionary<int, string> NumLevel =
        LevelNum.ToDictionary(kv => kv.Value, kv => kv.Key);
    private static readonly Dictionary<string, string> LevelLabel =
        new() { ["E"] = "E", ["D"] = "D", ["C"] = "C", ["B"] = "B", ["A"] = "A", ["M"] = "Masters" };

    private readonly AppConfig _cfg;

    public RatingEngine(AppConfig cfg) => _cfg = cfg;

    public RatingsOutput Compute(LeagueData league)
    {
        // Dictionary preserves insertion order (first appearance of a player) — like Python's defaultdict.
        var players = new Dictionary<string, Acc>();
        Acc Get(string name)
        {
            if (!players.TryGetValue(name, out var a))
            {
                a = new Acc(_cfg.Elo.Initial);
                players[name] = a;
            }
            return a;
        }

        foreach (var (t, m) in IterPlayed(league))
        {
            var a = m.Sides[0];
            var b = m.Sides[1];
            var winner = a.Won ? a : b;
            var discipline = m.Discipline;
            var level = m.Level;
            var combo = level != null ? $"{discipline}_{level}" : null;
            var kind = Events.RoundKind(m.Round);

            var mult = level != null && _cfg.Points.LevelMultiplier.TryGetValue(level, out var mm) ? mm : 1.0;
            var winPts = _cfg.Points.WinByRound[kind] * mult;

            foreach (var side in new[] { a, b })
            {
                var opp = ReferenceEquals(side, a) ? b : a;
                var won = ReferenceEquals(side, winner);
                foreach (var name in side.Players)
                {
                    var p = Get(name);
                    p.Tournaments.Add(t.Id);
                    p.Matches++;
                    if (won)
                    {
                        p.Wins++;
                        p.Points += winPts;
                        p.PointsByDiscipline[discipline] = p.PointsByDiscipline.GetValueOrDefault(discipline) + winPts;
                        if (level != null)
                        {
                            p.PointsByLevel[level] = p.PointsByLevel.GetValueOrDefault(level) + winPts;
                            p.PointsByCombo[combo!] = p.PointsByCombo.GetValueOrDefault(combo!) + winPts;
                        }
                    }
                    if (m.Walkover) p.Walkovers++;
                    if (level != null) p.LevelsPlayed[level] = p.LevelsPlayed.GetValueOrDefault(level) + 1;
                    p.History.Add(won ? "W" : "L");

                    Bump(p.ByDiscipline, discipline, won);
                    if (level != null) Bump(p.ByLevel, level, won);
                    if (combo != null) Bump(p.ByCombo, combo, won);

                    var games = ReferenceEquals(side, a)
                        ? m.Games.Select(g => new[] { g[0], g[1] }).ToList()
                        : m.Games.Select(g => new[] { g[1], g[0] }).ToList();
                    p.MatchLog.Add(new MatchLogEntry
                    {
                        Date = t.Date,
                        Event = m.Event,
                        Discipline = discipline,
                        Level = level,
                        Round = m.Round,
                        Won = won,
                        Walkover = m.Walkover,
                        Teammates = side.Players.Where(x => x != name).ToList(),
                        Opponents = opp.Players.ToList(),
                        Games = games,
                    });
                }
            }

            // Elo — walkovers excluded. Update the overall, discipline, level and
            // discipline+level (combo) ratings, e.g. "singles_D" for a singles-D match.
            if (!m.Walkover)
            {
                var scopes = combo != null
                    ? new[] { "overall", discipline, level!, combo }
                    : new[] { "overall", discipline };
                foreach (var scope in scopes)
                {
                    var ra = a.Players.Average(n => Get(n).GetElo(scope));
                    var rb = b.Players.Average(n => Get(n).GetElo(scope));
                    var ea = 1.0 / (1.0 + Math.Pow(10, (rb - ra) / _cfg.Elo.Scale));
                    var sa = ReferenceEquals(a, winner) ? 1.0 : 0.0;
                    var delta = _cfg.Elo.KFactor * (sa - ea);
                    foreach (var n in a.Players) { var p = Get(n); p.Elo[scope] = p.GetElo(scope) + delta; p.EloMatches[scope] = p.EloMatches.GetValueOrDefault(scope) + 1; }
                    foreach (var n in b.Players) { var p = Get(n); p.Elo[scope] = p.GetElo(scope) - delta; p.EloMatches[scope] = p.EloMatches.GetValueOrDefault(scope) + 1; }
                }
            }
        }

        foreach (var p in players.Values)
            p.Points += _cfg.Points.Participation * p.Tournaments.Count;

        InferLevels(players);

        return BuildOutput(league, players);
    }

    private void InferLevels(Dictionary<string, Acc> players)
    {
        var cfg = _cfg.LevelInference;
        foreach (var p in players.Values)
        {
            var total = p.LevelsPlayed.Values.Sum();
            if (total == 0) { p.Level = null; continue; }

            var avg = p.LevelsPlayed.Sum(kv => LevelNum[kv.Key] * (double)kv.Value) / total;
            var winrate = p.Matches > 0 ? (double)p.Wins / p.Matches : 0.5;
            if (winrate >= cfg.WinrateUp) avg += cfg.Adjustment;
            else if (winrate <= cfg.WinrateDown) avg -= cfg.Adjustment;

            var num = Math.Clamp((int)(avg + 0.5), 1, Levels.Length);
            p.Level = NumLevel[num];
            p.Trend = winrate >= cfg.WinrateUp ? "up"
                    : winrate <= cfg.WinrateDown ? "down"
                    : "stable";
        }
    }

    private static RatingsOutput BuildOutput(LeagueData league, Dictionary<string, Acc> players)
    {
        var output = new RatingsOutput
        {
            Tournaments = league.Tournaments.Select(t => new TournamentSummary
            {
                Id = t.Id,
                Name = t.Name,
                Date = t.Date,
                Matches = t.Matches.Count,
            }).ToList(),
        };

        // OrderByDescending is stable — on equal Elo the appearance order is kept (like Python's sorted).
        foreach (var (name, p) in players.OrderByDescending(kv => kv.Value.Elo["overall"]))
        {
            output.Players.Add(new PlayerOut
            {
                Name = name,
                Level = p.Level,
                LevelLabel = p.Level != null ? LevelLabel[p.Level] : null,
                LevelTrend = p.Trend,
                LevelsPlayed = new Dictionary<string, int>(p.LevelsPlayed),
                Elo = p.Elo.ToDictionary(kv => kv.Key, kv => Math.Round(kv.Value, 1)),
                EloMatches = new Dictionary<string, int>(p.EloMatches),
                Points = Math.Round(p.Points, 1),
                PointsByDiscipline = p.PointsByDiscipline.ToDictionary(kv => kv.Key, kv => Math.Round(kv.Value, 1)),
                PointsByLevel = p.PointsByLevel.ToDictionary(kv => kv.Key, kv => Math.Round(kv.Value, 1)),
                PointsByCombo = p.PointsByCombo.ToDictionary(kv => kv.Key, kv => Math.Round(kv.Value, 1)),
                Tournaments = p.Tournaments.Count,
                Matches = p.Matches,
                Wins = p.Wins,
                Losses = p.Matches - p.Wins,
                Winrate = p.Matches > 0 ? Math.Round((double)p.Wins / p.Matches, 3) : 0,
                Walkovers = p.Walkovers,
                ByDiscipline = p.ByDiscipline.ToDictionary(kv => kv.Key, kv => ToStat(kv.Value)),
                ByLevel = p.ByLevel.ToDictionary(kv => kv.Key, kv => ToStat(kv.Value)),
                ByCombo = p.ByCombo.ToDictionary(kv => kv.Key, kv => ToStat(kv.Value)),
                Form = Last(p.History, 8),
                MatchLog = p.MatchLog,
            });
        }
        return output;
    }

    private static List<string> Last(List<string> src, int n) =>
        src.Skip(Math.Max(0, src.Count - n)).ToList();

    private static DisciplineStat ToStat(DiscAcc d) => new()
    {
        Matches = d.Matches,
        Wins = d.Wins,
        Losses = d.Matches - d.Wins,
        Winrate = d.Matches > 0 ? Math.Round((double)d.Wins / d.Matches, 3) : 0,
        Form = Last(d.History, 8),
    };

    private static void Bump(Dictionary<string, DiscAcc> dict, string key, bool won)
    {
        if (!dict.TryGetValue(key, out var d))
        {
            d = new DiscAcc();
            dict[key] = d;
        }
        d.Matches++;
        if (won) d.Wins++;
        d.History.Add(won ? "W" : "L");
    }

    /// <summary>Matches in chronological order that are eligible for rating.</summary>
    private static IEnumerable<(Tournament, Match)> IterPlayed(LeagueData league)
    {
        foreach (var t in league.Tournaments)
            foreach (var m in t.Matches)
            {
                if (m.Bye) continue;
                if (m.Sides.Count != 2) continue;
                var a = m.Sides[0];
                var b = m.Sides[1];
                if (a.Players.Count == 0 || b.Players.Count == 0) continue; // unresolved placeholder
                if (!a.Won && !b.Won) continue;                             // match not played
                yield return (t, m);
            }
    }

    private sealed class Acc
    {
        private readonly double _initialElo;

        public Dictionary<string, double> Elo { get; } = new();
        public Dictionary<string, int> EloMatches { get; } = new();
        public double Points { get; set; }
        public Dictionary<string, double> PointsByDiscipline { get; } = new();
        public Dictionary<string, double> PointsByLevel { get; } = new();
        public Dictionary<string, double> PointsByCombo { get; } = new();
        public HashSet<string> Tournaments { get; } = new();
        public int Matches { get; set; }
        public int Wins { get; set; }
        public int Walkovers { get; set; }
        public Dictionary<string, int> LevelsPlayed { get; } = new();
        public List<string> History { get; } = new();
        public Dictionary<string, DiscAcc> ByDiscipline { get; } = new();
        public Dictionary<string, DiscAcc> ByLevel { get; } = new();
        public Dictionary<string, DiscAcc> ByCombo { get; } = new();
        public List<MatchLogEntry> MatchLog { get; } = new();
        public string? Level { get; set; }
        public string? Trend { get; set; }

        public Acc(double initialElo)
        {
            _initialElo = initialElo;
            // "overall" is always touched (every played match), so seed it eagerly;
            // every other scope ("singles", "D", "singles_D"...) is seeded lazily by GetElo.
            Elo["overall"] = initialElo;
            EloMatches["overall"] = 0;
        }

        /// <summary>Elo for a scope, initializing it to the starting value on first use.</summary>
        public double GetElo(string scope) => Elo.TryGetValue(scope, out var v) ? v : (Elo[scope] = _initialElo);
    }

    private sealed class DiscAcc
    {
        public int Matches { get; set; }
        public int Wins { get; set; }
        public List<string> History { get; } = new();
    }
}
