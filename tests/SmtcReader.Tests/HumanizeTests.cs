using SmtcReader.Formatting;
using Xunit;

namespace SmtcReader.Tests;

public class HumanizeTests
{
    [Fact]
    public void Duration_formats_hours_minutes_and_milliseconds()
    {
        Assert.Equal("00:05:01.975", Humanize.Duration(TimeSpan.FromSeconds(301.975)));
    }

    [Fact]
    public void Duration_keeps_running_past_an_hour()
    {
        Assert.Equal("01:02:03.004", Humanize.Duration(new TimeSpan(0, 1, 2, 3, 4)));
    }

    [Fact]
    public void Duration_does_not_wrap_at_twenty_four_hours()
    {
        // Several apps report a stopwatch, not a clip position.
        Assert.Equal("26:03:04.000", Humanize.Duration(new TimeSpan(1, 2, 3, 4)));
    }

    [Fact]
    public void Duration_of_null_is_a_dash()
    {
        Assert.Equal(Humanize.Dash, Humanize.Duration(null));
    }

    [Theory]
    [InlineData(512L, "512 B")]
    [InlineData(2048L, "2.00 KB")]
    [InlineData(1638454L, "1.56 MB")]
    public void Size_is_scaled(long bytes, string expected)
    {
        Assert.Equal(expected, Humanize.Size(bytes));
    }

    [Fact]
    public void Cell_treats_placeholders_as_missing_and_escapes_pipes()
    {
        Assert.Equal(Humanize.Dash, Humanize.Cell(null));
        Assert.Equal(Humanize.Dash, Humanize.Cell(string.Empty));
        Assert.Equal(Humanize.Dash, Humanize.Cell("<null>"));
        Assert.Equal(Humanize.Dash, Humanize.Cell("<empty>"));
        Assert.Equal("a\\|b", Humanize.Cell("a|b"));
    }

    [Fact]
    public void Timestamp_hides_the_year_1601_that_some_apps_report()
    {
        Assert.Equal(Humanize.Dash, Humanize.Timestamp(new DateTimeOffset(1601, 1, 1, 8, 0, 0, TimeSpan.FromHours(8))));
        Assert.Equal(
            "2026-10-06 19:01:54.779 +08:00",
            Humanize.Timestamp(new DateTimeOffset(2026, 10, 6, 19, 1, 54, 779, TimeSpan.FromHours(8))));
    }

    [Fact]
    public void Console_keeps_null_and_empty_distinguishable()
    {
        Assert.Equal("<null>", Humanize.Console(null));
        Assert.Equal("<empty>", Humanize.Console(string.Empty));
        Assert.Equal("Playing", Humanize.Console("Playing"));
    }
}
