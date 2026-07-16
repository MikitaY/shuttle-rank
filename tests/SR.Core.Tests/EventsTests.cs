using SR.Core;

namespace SR.Core.Tests;

public class EventsTests
{
    [Theory]
    [InlineData("SE - Group A", "SE", "Group A")]
    [InlineData("XDD", "XDD", null)]
    [InlineData("MASTER+ - Group B", "MASTER+", "Group B")]
    public void ParseEventCode_splits_event_and_group(string input, string evt, string? group)
    {
        var (e, g) = Events.ParseEventCode(input);
        Assert.Equal(evt, e);
        Assert.Equal(group, g);
    }

    [Theory]
    [InlineData("SE", "singles", "E")]
    [InlineData("SD", "singles", "D")]
    [InlineData("DC", "doubles", "C")]
    [InlineData("DB", "doubles", "B")]
    [InlineData("XDB", "mixed", "B")]
    [InlineData("XDD", "mixed", "D")]
    [InlineData("WSD", "singles", "D")]
    [InlineData("WDD", "doubles", "D")]
    [InlineData("MASTER+", "doubles", "M")]
    public void EventMeta_maps_discipline_and_level(string code, string discipline, string? level)
    {
        var (d, l) = Events.EventMeta(code);
        Assert.Equal(discipline, d);
        Assert.Equal(level, l);
    }

    [Theory]
    [InlineData("Final", "final")]
    [InlineData("Semi final", "semi")]
    [InlineData("Quarter final", "quarter")]
    [InlineData("Round 1", "group")]
    [InlineData(null, "group")]
    public void RoundKind_classifies_stage(string? round, string expected)
    {
        Assert.Equal(expected, Events.RoundKind(round));
    }
}
