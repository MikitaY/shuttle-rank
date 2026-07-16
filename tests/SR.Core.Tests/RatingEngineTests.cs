using SR.Core;

namespace SR.Core.Tests;

public class RatingEngineTests
{
    private static AppConfig Config() => new()
    {
        Elo = new EloConfig { Initial = 1500, KFactor = 32, Scale = 400 },
        Points = new PointsConfig
        {
            Participation = 5,
            WinByRound = new() { ["group"] = 10, ["quarter"] = 15, ["semi"] = 20, ["final"] = 30 },
            LevelMultiplier = new() { ["E"] = 1.0, ["D"] = 1.25, ["C"] = 1.55, ["B"] = 1.9, ["M"] = 2.3 },
        },
        LevelInference = new LevelInferenceConfig { WinrateUp = 0.65, WinrateDown = 0.35, Adjustment = 0.4 },
    };

    private static LeagueData SinglesMatch(bool walkover = false) => new()
    {
        Tournaments =
        {
            new Tournament
            {
                Id = "t1", Name = "T", Date = "2026-01-01",
                Matches =
                {
                    new Match
                    {
                        DrawName = "SE - Group A", Event = "SE", Group = "Group A",
                        Discipline = "singles", Level = "E", Round = "Round 1",
                        Sides =
                        {
                            new Side { Players = { "A" }, Won = true },
                            new Side { Players = { "B" }, Won = false },
                        },
                        Games = walkover ? new() : new() { new[] { 21, 10 }, new[] { 21, 15 } },
                        Walkover = walkover,
                    },
                },
            },
        },
    };

    private static PlayerOut Player(RatingsOutput r, string name) =>
        r.Players.Single(p => p.Name == name);

    [Fact]
    public void Elo_moves_symmetrically_from_equal_start()
    {
        var r = new RatingEngine(Config()).Compute(SinglesMatch());
        var a = Player(r, "A");
        var b = Player(r, "B");

        // Равный старт => ожидание 0.5 => дельта 32*(1-0.5)=16.
        Assert.Equal(1516.0, a.Elo["overall"]);
        Assert.Equal(1516.0, a.Elo["singles"]);
        Assert.Equal(1484.0, b.Elo["overall"]);
        Assert.Equal(1, a.EloMatches["singles"]);
        Assert.Equal(0, a.EloMatches["doubles"]);
    }

    [Fact]
    public void Points_count_win_plus_participation()
    {
        var r = new RatingEngine(Config()).Compute(SinglesMatch());
        // Победа в группе (10 × ×1.0) + участие 5 = 15; проигравший — только участие 5.
        Assert.Equal(15.0, Player(r, "A").Points);
        Assert.Equal(5.0, Player(r, "B").Points);
        Assert.Equal(10.0, Player(r, "A").PointsByDiscipline["singles"]);
    }

    [Fact]
    public void Level_inferred_with_winrate_trend()
    {
        var r = new RatingEngine(Config()).Compute(SinglesMatch());
        var a = Player(r, "A");
        var b = Player(r, "B");
        Assert.Equal("E", a.Level);
        Assert.Equal("up", a.LevelTrend);
        Assert.Equal("down", b.LevelTrend);
    }

    [Fact]
    public void MatchLog_flips_games_for_second_side()
    {
        var r = new RatingEngine(Config()).Compute(SinglesMatch());
        Assert.Equal(new[] { 21, 10 }, Player(r, "A").MatchLog[0].Games[0]);
        Assert.Equal(new[] { 10, 21 }, Player(r, "B").MatchLog[0].Games[0]);
        Assert.Equal(new[] { "B" }, Player(r, "A").MatchLog[0].Opponents.ToArray());
    }

    [Fact]
    public void Walkover_counts_stats_but_not_elo()
    {
        var r = new RatingEngine(Config()).Compute(SinglesMatch(walkover: true));
        var a = Player(r, "A");
        Assert.Equal(1500.0, a.Elo["overall"]);   // Elo не меняется
        Assert.Equal(0, a.EloMatches["overall"]);
        Assert.Equal(1, a.Matches);                 // но матч засчитан
        Assert.Equal(1, a.Walkovers);
    }
}
