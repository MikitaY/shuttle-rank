namespace SR.Core.Tests;

public class DriveVolanTests
{
    [Theory]
    [InlineData("Группа A", "Group A", null)]
    [InlineData("Финал", null, "Final")]
    [InlineData("Полуфинал", null, "Semi final")]
    [InlineData("Четвертьфинал", null, "Quarter final")]
    [InlineData("Матч за 3 место", null, "3rd/4th place")]
    [InlineData("Матч а 5-8 место", null, "Places 5-8")]
    [InlineData("Матч за 7 место", null, "Places 7")]
    [InlineData(null, null, null)]
    public void ParseRound_maps_site_labels(string? label, string? group, string? round)
    {
        var (g, r) = DriveVolan.ParseRound(label);
        Assert.Equal(group, g);
        Assert.Equal(round, r);
    }

    [Fact]
    public void ParseRound_keeps_stage_points_in_line_with_tournamentsoftware()
    {
        Assert.Equal("final", Events.RoundKind(DriveVolan.ParseRound("Финал").Round));
        Assert.Equal("semi", Events.RoundKind(DriveVolan.ParseRound("Полуфинал").Round));
        Assert.Equal("quarter", Events.RoundKind(DriveVolan.ParseRound("Четвертьфинал").Round));
        Assert.Equal("group", Events.RoundKind(DriveVolan.ParseRound("Матч за 3 место").Round));
    }

    [Fact]
    public void ParseScore_reads_every_game()
    {
        var games = DriveVolan.ParseScore("15-21, 21-10, 21-17");
        Assert.Equal(new[] { new[] { 15, 21 }, new[] { 21, 10 }, new[] { 21, 17 } }, games);
    }

    [Theory]
    [InlineData("21-0, 21-0", true)]
    [InlineData("0-21, 0-21", true)]
    [InlineData("21-0, 21-1", false)]
    [InlineData("21-12, 21-10", false)]
    public void IsWalkover_detects_straight_zero_results(string score, bool expected) =>
        Assert.Equal(expected, DriveVolan.IsWalkover(DriveVolan.ParseScore(score)));

    [Fact]
    public void ToTournament_maps_names_and_winner()
    {
        var export = new DriveVolanExport
        {
            Id = 5,
            Name = "Драйвовый волан",
            Date = "2026-10-03",
            Matches =
            [
                new DriveVolanMatch
                {
                    Category = "XDC", Round = "Группа B",
                    SideA = ["Иванов Иван", "Петрова Анна"], SideB = ["Smith John", "Сидорова Ольга"],
                    Winner = "b", Score = "13-15, 15-6, 12-15",
                },
            ],
        };
        var names = new Dictionary<string, string>
        {
            ["Иванов Иван"] = "Иван Иванов",
            ["Петрова Анна"] = "Анна Петрова",
            ["Smith John"] = "Джон Смит",
            ["Сидорова Ольга"] = "Ольга Сидорова",
        };

        var t = DriveVolan.ToTournament(export, names);

        Assert.Equal("drivevolan-5", t.Id);
        var m = Assert.Single(t.Matches);
        Assert.Equal("XDC - Group B", m.DrawName);
        Assert.Equal(("mixed", "C"), (m.Discipline, m.Level));
        Assert.Equal(["Иван Иванов", "Анна Петрова"], m.Sides[0].Players);
        Assert.False(m.Sides[0].Won);
        Assert.True(m.Sides[1].Won);
        Assert.False(m.Walkover);
    }

    [Fact]
    public void ToTournament_rejects_players_missing_from_the_map()
    {
        var export = new DriveVolanExport
        {
            Id = 1,
            Matches = [new DriveVolanMatch { Category = "SB", SideA = ["A"], SideB = ["B"], Winner = "a", Score = "21-1, 21-1" }],
        };
        var ex = Assert.Throws<InvalidOperationException>(() =>
            DriveVolan.ToTournament(export, new Dictionary<string, string> { ["A"] = "A" }));
        Assert.Contains("B", ex.Message);
    }
}
