namespace SR.Core;

/// <summary>The cross-category ("сквозной") rating: a single number per player built from
/// every match they played, in any discipline and any level.
///
/// The trick that makes one shared scale work is the seed — a player starts at the midpoint
/// of the category they entered the league in (E 1100, D 1300, C 1500 …), so a D player
/// beating a C player is worth more than beating another D player from the very first match.
/// Categories are chess-style bands 200 points wide, with a confirmation threshold
/// (no promotion on a provisional rating) and a demotion buffer (no bouncing).</summary>
public sealed class UnifiedRater
{
    private readonly UnifiedConfig _cfg;
    private readonly List<(string Key, double Floor)> _ladder;
    private readonly Dictionary<string, (double Seed, string? Level)> _seeds;
    private readonly Dictionary<string, State> _state = new();

    // Rating snapshots are taken per tournament, so the frontend can draw a trajectory.
    private string? _tournamentId;
    private string? _tournamentDate;
    private readonly HashSet<string> _tournamentPlayers = new();

    public UnifiedRater(UnifiedConfig cfg, LeagueData league)
    {
        _cfg = cfg;
        _ladder = cfg.Ladder
            .Select(k => (Key: k, Floor: cfg.CategoryFloors.GetValueOrDefault(k)))
            .OrderBy(x => x.Floor)
            .ToList();
        if (_ladder.Count == 0) throw new InvalidOperationException("unified.ladder is empty");
        _seeds = SeedPlayers(cfg, league);
    }

    /// <summary>Per-player rating change for a match, or null when the match doesn't count.
    /// Must be called before <see cref="Commit"/> — it reads pre-match ratings.</summary>
    public Dictionary<string, double>? Rate(Match m, Side a, Side b)
    {
        if (m.Walkover) return null;

        var ra = a.Players.Average(Rating);
        var rb = b.Players.Average(Rating);
        var ea = 1.0 / (1.0 + Math.Pow(10, (rb - ra) / _cfg.Scale));

        // A doubles result says less about you than a singles one — half of it is your partner.
        var weight = m.Discipline == "singles" ? 1.0 : _cfg.DoublesWeight;

        var deltas = new Dictionary<string, double>();
        Fill(a, a.Won ? 1.0 : 0.0, ea);
        Fill(b, b.Won ? 1.0 : 0.0, 1.0 - ea);
        return deltas;

        void Fill(Side side, double actual, double expected)
        {
            foreach (var name in side.Players)
                deltas[name] = KFor(name) * weight * (actual - expected);
        }
    }

    /// <summary>Applies the deltas from <see cref="Rate"/> and re-evaluates categories.</summary>
    public void Commit(Tournament t, Dictionary<string, double> deltas)
    {
        if (_tournamentId != t.Id)
        {
            Snapshot();
            _tournamentId = t.Id;
            _tournamentDate = t.Date;
            _tournamentPlayers.Clear();
        }

        foreach (var (name, delta) in deltas)
        {
            var s = Get(name);
            s.Rating += delta;
            s.Matches++;
            s.Peak = Math.Max(s.Peak, s.Rating);
            UpdateCategory(s, t.Date);
            _tournamentPlayers.Add(name);
        }
    }

    /// <summary>Closes the last tournament's snapshot. Call once after the final match.</summary>
    public void Finish() => Snapshot();

    /// <summary>Current rating — the seed for a player who hasn't been rated yet.</summary>
    public double Rating(string name) => Get(name).Rating;

    /// <summary>Result for the output model, or null for a player with no rated match
    /// (everything they played was a walkover).</summary>
    public UnifiedOut? Result(string name)
    {
        if (!_state.TryGetValue(name, out var s) || s.Matches == 0) return null;

        var i = IndexOf(s.Category);
        var floor = _ladder[i].Floor;
        var next = i + 1 < _ladder.Count ? _ladder[i + 1] : ((string Key, double Floor)?)null;
        var provisional = s.Matches < _cfg.ProvisionalMatches;

        // Progress through the current band. The bottom band is open-ended, so measure it
        // backwards from the next floor instead of from its nominal zero.
        var start = i > 0 ? floor : (next?.Floor ?? floor) - _cfg.BandWidth;
        var span = (next?.Floor ?? start + _cfg.BandWidth) - start;
        var progress = span > 0 ? Math.Clamp((s.Rating - start) / span, 0, 1) : 1;

        return new UnifiedOut
        {
            Rating = Math.Round(s.Rating, 1),
            Seed = Math.Round(s.Seed, 1),
            SeedLevel = s.SeedLevel,
            Matches = s.Matches,
            Category = s.Category,
            NextCategory = next?.Key,
            Floor = floor,
            NextFloor = next?.Floor,
            Progress = Math.Round(progress, 3),
            Provisional = provisional,
            // ▲ within reach of the next floor, ▼ hanging on the edge of its own.
            Status = provisional ? "provisional"
                   : next is { } n && s.Rating >= n.Floor - _cfg.TransitionZone ? "promotion"
                   : i > 0 && s.Rating < floor + _cfg.TransitionZone ? "demotion"
                   : "stable",
            Peak = Math.Round(s.Peak, 1),
            CategorySince = s.CategoryChangedAt,
            History = s.History,
        };
    }

    /// <summary>The ladder's thresholds, for the frontend to render.</summary>
    public UnifiedParams Params() => new()
    {
        ProvisionalMatches = _cfg.ProvisionalMatches,
        DemotionBuffer = _cfg.DemotionBuffer,
        TransitionZone = _cfg.TransitionZone,
        Categories = _ladder
            .Select(b => new UnifiedCategory
            {
                Key = b.Key,
                Floor = b.Floor,
                Seed = _cfg.SeedByLevel.GetValueOrDefault(b.Key, _cfg.DefaultSeed),
            })
            .ToList(),
    };

    /// <summary>Rating a player's results move at: fast while provisional, slower once settled.</summary>
    private double KFor(string name)
    {
        var played = Get(name).Matches;
        if (played < _cfg.ProvisionalMatches) return _cfg.KProvisional;
        if (played < _cfg.DevelopingMatches) return _cfg.KDeveloping;
        return _cfg.KSettled;
    }

    private void UpdateCategory(State s, string? date)
    {
        // A provisional rating can't earn a category — it just sits in the seed's band.
        if (s.Matches < _cfg.ProvisionalMatches)
        {
            s.Category = BandOf(s.Seed);
            return;
        }

        var i = IndexOf(s.Category);
        var from = i;
        while (i + 1 < _ladder.Count && s.Rating >= _ladder[i + 1].Floor) i++;
        while (i > 0 && s.Rating < _ladder[i].Floor - _cfg.DemotionBuffer) i--;
        if (i == from) return;

        s.Category = _ladder[i].Key;
        s.CategoryChangedAt = date;
    }

    /// <summary>Snapshots everyone who played in the tournament that just ended.</summary>
    private void Snapshot()
    {
        if (_tournamentId == null) return;
        foreach (var name in _tournamentPlayers)
        {
            var s = _state[name];
            s.History.Add(new UnifiedPoint { Date = _tournamentDate, Rating = Math.Round(s.Rating, 1) });
        }
    }

    private State Get(string name)
    {
        if (_state.TryGetValue(name, out var s)) return s;

        var (seed, level) = _seeds.TryGetValue(name, out var v) ? v : (_cfg.DefaultSeed, null);
        s = new State
        {
            Rating = seed,
            Peak = seed,
            Seed = seed,
            SeedLevel = level,
            Category = BandOf(seed),
        };
        _state[name] = s;
        return s;
    }

    private string BandOf(double rating)
    {
        var i = 0;
        while (i + 1 < _ladder.Count && rating >= _ladder[i + 1].Floor) i++;
        return _ladder[i].Key;
    }

    private int IndexOf(string category)
    {
        var i = _ladder.FindIndex(x => x.Key == category);
        return i < 0 ? 0 : i;
    }

    /// <summary>Seeds every player from the categories they played in their first tournament —
    /// a mean of the seeds of those draws, so someone who entered in both SD and DC starts
    /// between D and C.</summary>
    private static Dictionary<string, (double Seed, string? Level)> SeedPlayers(UnifiedConfig cfg, LeagueData league)
    {
        var firstTournament = new Dictionary<string, string>();
        var acc = new Dictionary<string, (double Sum, int Count, Dictionary<string, int> Levels)>();

        foreach (var (t, m) in RatingEngine.IterPlayed(league))
            foreach (var side in m.Sides)
                foreach (var name in side.Players)
                {
                    if (!firstTournament.TryGetValue(name, out var id))
                    {
                        firstTournament[name] = id = t.Id;
                        acc[name] = (0, 0, new Dictionary<string, int>());
                    }
                    if (id != t.Id) continue;   // only the debut tournament shapes the seed

                    var a = acc[name];
                    a.Sum += m.Level != null && cfg.SeedByLevel.TryGetValue(m.Level, out var seed)
                        ? seed
                        : cfg.DefaultSeed;
                    a.Count++;
                    if (m.Level != null) a.Levels[m.Level] = a.Levels.GetValueOrDefault(m.Level) + 1;
                    acc[name] = a;
                }

        return acc.ToDictionary(
            kv => kv.Key,
            kv =>
            {
                var (sum, count, levels) = kv.Value;
                // The label shown as "entered as X": the level played most in the debut,
                // ties going to the weaker one.
                var level = levels
                    .OrderByDescending(l => l.Value)
                    .ThenBy(l => cfg.SeedByLevel.GetValueOrDefault(l.Key, cfg.DefaultSeed))
                    .Select(l => l.Key)
                    .FirstOrDefault();
                return (count > 0 ? sum / count : cfg.DefaultSeed, level);
            });
    }

    private sealed class State
    {
        public double Rating { get; set; }
        public double Peak { get; set; }
        public double Seed { get; set; }
        public string? SeedLevel { get; set; }
        public int Matches { get; set; }
        public string Category { get; set; } = "";
        public string? CategoryChangedAt { get; set; }
        public List<UnifiedPoint> History { get; } = new();
    }
}
