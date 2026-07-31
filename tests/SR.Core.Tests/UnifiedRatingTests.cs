namespace SR.Core.Tests;

public class UnifiedRatingTests
{
    private static AppConfig Config() => new()
    {
        Elo = new EloConfig { Initial = 1500, KFactor = 32, Scale = 400 },
        Points = new PointsConfig
        {
            Participation = 5,
            WinByRound = new Dictionary<string, double> { ["group"] = 10, ["quarter"] = 15, ["semi"] = 20, ["final"] = 30 },
            LevelMultiplier = new Dictionary<string, double> { ["E"] = 1.0, ["D"] = 1.25, ["C"] = 1.55 },
        },
        LevelInference = new LevelInferenceConfig(),
        Unified = new UnifiedConfig(),   // defaults mirror config.json
    };

    /// <summary>A singles match between two players in a draw of the given level.</summary>
    private static Match Singles(string winner, string loser, string level, int seq = 0) => new()
    {
        Seq = seq,
        DrawName = $"S{level}", Event = $"S{level}",
        Discipline = "singles", Level = level, Round = "Round 1",
        Sides =
        {
            new Side { Players = { winner }, Won = true },
            new Side { Players = { loser }, Won = false },
        },
        Games = { new[] { 21, 10 }, new[] { 21, 15 } },
    };

    private static Match Doubles(string[] winners, string[] losers, string level) => new()
    {
        DrawName = $"D{level}", Event = $"D{level}",
        Discipline = "doubles", Level = level, Round = "Round 1",
        Sides =
        {
            new Side { Players = { winners[0], winners[1] }, Won = true },
            new Side { Players = { losers[0], losers[1] }, Won = false },
        },
        Games = { new[] { 21, 10 }, new[] { 21, 15 } },
    };

    private static LeagueData League(params (string Id, string Date, Match[] Matches)[] tournaments) => new()
    {
        Tournaments = tournaments
            .Select(t => new Tournament { Id = t.Id, Name = t.Id, Date = t.Date, Matches = t.Matches.ToList() })
            .ToList(),
    };

    private static UnifiedOut Unified(RatingsOutput r, string name) =>
        r.Players.Single(p => p.Name == name).Unified!;

    [Fact]
    public void Seed_comes_from_the_level_the_player_entered_in()
    {
        var league = League(("t1", "2026-01-01", [Singles("A", "B", "E"), Singles("C", "D", "B")]));
        var r = new RatingEngine(Config()).Compute(league);

        Assert.Equal(1100, Unified(r, "A").Seed);
        Assert.Equal("E", Unified(r, "A").SeedLevel);
        Assert.Equal(1700, Unified(r, "C").Seed);
        Assert.Equal("B", Unified(r, "C").SeedLevel);
    }

    [Fact]
    public void Seed_averages_the_levels_of_the_debut_tournament()
    {
        // Entered in both a D draw (1300) and a C draw (1500) => starts between them.
        var league = League(("t1", "2026-01-01", [Singles("A", "B", "D"), Singles("A", "C", "C")]));
        var r = new RatingEngine(Config()).Compute(league);

        Assert.Equal(1400, Unified(r, "A").Seed);
    }

    [Fact]
    public void Later_tournaments_do_not_change_the_seed()
    {
        var league = League(
            ("t1", "2026-01-01", [Singles("A", "B", "E")]),
            ("t2", "2026-02-01", [Singles("A", "C", "A")]));
        var r = new RatingEngine(Config()).Compute(league);

        Assert.Equal(1100, Unified(r, "A").Seed);
    }

    [Fact]
    public void Beating_an_equal_opponent_moves_the_rating_by_half_of_k()
    {
        var league = League(("t1", "2026-01-01", [Singles("A", "B", "D")]));
        var r = new RatingEngine(Config()).Compute(league);

        // Equal seeds => expected .5 => provisional K 40 * .5 = 20.
        Assert.Equal(1320, Unified(r, "A").Rating);
        Assert.Equal(1280, Unified(r, "B").Rating);
    }

    [Fact]
    public void Beating_a_higher_category_opponent_is_worth_more()
    {
        var underdog = League(("t1", "2026-01-01", [Singles("A", "B", "D"), Singles("A", "C", "D")]));
        underdog.Tournaments[0].Matches[1].Sides[1].Players[0] = "C";   // C entered in a B draw below
        underdog.Tournaments[0].Matches.Add(Singles("C", "X", "B"));

        var r = new RatingEngine(Config()).Compute(underdog);
        var log = r.Players.Single(p => p.Name == "A").MatchLog;

        // Match 1 vs an equal D, match 2 vs C who is seeded 200 points higher.
        Assert.True(log[1].UnifiedDelta > log[0].UnifiedDelta);
    }

    [Fact]
    public void Doubles_results_move_the_rating_less_than_singles()
    {
        var singles = League(("t1", "2026-01-01", [Singles("A", "B", "D")]));
        var doubles = League(("t1", "2026-01-01", [Doubles(["A", "P"], ["B", "Q"], "D")]));

        var single = Unified(new RatingEngine(Config()).Compute(singles), "A").Rating - 1300;
        var pair = Unified(new RatingEngine(Config()).Compute(doubles), "A").Rating - 1300;

        Assert.Equal(20, single);
        Assert.Equal(15, pair);          // 0.75 weight
    }

    [Fact]
    public void Walkovers_are_not_rated_and_carry_no_delta()
    {
        var league = League(("t1", "2026-01-01", [Singles("A", "B", "D")]));
        league.Tournaments[0].Matches[0].Walkover = true;
        league.Tournaments[0].Matches[0].Games.Clear();

        var r = new RatingEngine(Config()).Compute(league);

        Assert.Null(r.Players.Single(p => p.Name == "A").Unified);
        Assert.Null(r.Players.Single(p => p.Name == "A").MatchLog[0].UnifiedDelta);
    }

    [Fact]
    public void Category_stays_in_the_seed_band_while_provisional()
    {
        // Nine straight wins over fresh D opponents: rating is well past the C floor,
        // but the category can't move before the confirmation threshold (10 matches).
        var matches = Enumerable.Range(0, 9).Select(i => Singles("A", $"B{i}", "D")).ToArray();
        var r = new RatingEngine(Config()).Compute(League(("t1", "2026-01-01", matches)));
        var u = Unified(r, "A");

        Assert.True(u.Rating > 1400);
        Assert.Equal("D", u.Category);
        Assert.True(u.Provisional);
        Assert.Equal("provisional", u.Status);
    }

    [Fact]
    public void Category_is_promoted_once_the_rating_is_confirmed()
    {
        var matches = Enumerable.Range(0, 12).Select(i => Singles("A", $"B{i}", "D")).ToArray();
        var r = new RatingEngine(Config()).Compute(League(("t1", "2026-01-01", matches)));
        var u = Unified(r, "A");

        Assert.False(u.Provisional);
        Assert.Equal("C", u.Category);
        Assert.Equal("B", u.NextCategory);
        Assert.Equal(1400, u.Floor);
        Assert.Equal("2026-01-01", u.CategorySince);
    }

    [Fact]
    public void Approaching_the_next_floor_reads_as_a_transition()
    {
        var cfg = Config();
        cfg.Unified.ProvisionalMatches = 2;

        // Three wins from a D seed land just inside the 60-point zone below the C floor.
        var matches = Enumerable.Range(0, 3).Select(i => Singles("A", $"B{i}", "D")).ToArray();
        var u = Unified(new RatingEngine(cfg).Compute(League(("t1", "2026-01-01", matches))), "A");

        Assert.InRange(u.Rating, 1340, 1400);
        Assert.Equal("D", u.Category);
        Assert.Equal("C", u.NextCategory);
        Assert.Equal("promotion", u.Status);
        Assert.InRange(u.Progress, 0.7, 0.8);   // (rating - 1200) / 200
    }

    [Fact]
    public void A_dip_below_the_floor_does_not_demote_inside_the_buffer()
    {
        // Seeded C (1500), nine losses to fellow C players and one win back:
        // the rating ends below the C floor but inside the 50-point buffer.
        var matches = Enumerable.Range(0, 9).Select(i => Singles($"L{i}", "A", "C")).ToList();
        matches.Add(Singles("A", "W", "C"));

        var u = Unified(new RatingEngine(Config()).Compute(League(("t1", "2026-01-01", matches.ToArray()))), "A");

        Assert.InRange(u.Rating, 1350, 1400);
        Assert.False(u.Provisional);
        Assert.Equal("C", u.Category);        // held by the demotion buffer
    }

    [Fact]
    public void Falling_past_the_buffer_demotes()
    {
        // Same C seed, twelve losses and no win back — past the buffer, down to D.
        var matches = Enumerable.Range(0, 12).Select(i => Singles($"L{i}", "A", "C")).ToArray();
        var u = Unified(new RatingEngine(Config()).Compute(League(("t1", "2026-01-01", matches))), "A");

        Assert.True(u.Rating < 1350);
        Assert.Equal("D", u.Category);
    }

    [Fact]
    public void K_factor_slows_down_as_matches_pile_up()
    {
        var cfg = Config();
        var matches = Enumerable.Range(0, 11).Select(i => Singles("A", $"B{i}", "D")).ToArray();
        var r = new RatingEngine(cfg).Compute(League(("t1", "2026-01-01", matches)));
        var log = r.Players.Single(p => p.Name == "A").MatchLog;

        // Match 1 (provisional, K=40) vs match 11 (developing, K=32) against equally
        // seeded fresh opponents — the same expectation, a smaller step.
        Assert.Equal(20, log[0].UnifiedDelta);
        Assert.True(log[10].UnifiedDelta < 20);
    }

    [Fact]
    public void History_records_the_rating_after_every_tournament_played()
    {
        var league = League(
            ("t1", "2026-01-01", [Singles("A", "B", "D")]),
            ("t2", "2026-02-01", [Singles("A", "C", "D")]),
            ("t3", "2026-03-01", [Singles("X", "Y", "D")]));
        var u = Unified(new RatingEngine(Config()).Compute(league), "A");

        Assert.Equal(2, u.History.Count);
        Assert.Equal("2026-01-01", u.History[0].Date);
        Assert.Equal("2026-02-01", u.History[1].Date);
        Assert.Equal(u.Rating, u.History[1].Rating);
    }

    [Fact]
    public void Match_log_carries_the_running_rating()
    {
        var league = League(("t1", "2026-01-01", [Singles("A", "B", "D"), Singles("A", "C", "D")]));
        var log = new RatingEngine(Config()).Compute(league).Players.Single(p => p.Name == "A").MatchLog;

        Assert.Equal(1320, log[0].UnifiedAfter);
        Assert.Equal(log[1].UnifiedAfter, 1320 + log[1].UnifiedDelta);
    }
}
