using SmtcReader.Rendering;
using Xunit;

namespace SmtcReader.Tests;

public class TextLayoutTests
{
    [Theory]
    [InlineData("abc", 3)]
    [InlineData("播放状态", 8)]
    [InlineData("播放 status", 11)]
    [InlineData("", 0)]
    public void DisplayWidth_counts_wide_characters_as_two(string value, int expected)
    {
        Assert.Equal(expected, TextLayout.DisplayWidth(value));
    }

    [Fact]
    public void Pad_aligns_wide_and_narrow_labels_to_the_same_column()
    {
        var chinese = TextLayout.Pad("状态", 12) + ":";
        var english = TextLayout.Pad("Status", 12) + ":";

        Assert.Equal(TextLayout.DisplayWidth(chinese), TextLayout.DisplayWidth(english));
    }

    [Fact]
    public void Pad_leaves_labels_longer_than_the_column_alone()
    {
        Assert.Equal("Playback status", TextLayout.Pad("Playback status", 12));
    }

    [Fact]
    public void Truncate_keeps_the_result_within_the_budget()
    {
        var truncated = TextLayout.Truncate("这是一个很长的标题需要被截断", 10);

        Assert.True(TextLayout.DisplayWidth(truncated) <= 10);
        Assert.EndsWith("…", truncated, StringComparison.Ordinal);
    }
}
