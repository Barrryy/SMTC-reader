using SmtcReader.Rendering;
using Xunit;

namespace SmtcReader.Tests;

/// <summary>
/// Keeps docs/sample-report.md honest. The sample is documentation, so without this
/// test it would silently drift away from what the renderer prints the first time a
/// column changes.
/// </summary>
public class SampleReportTests
{
    [Fact]
    public void The_documented_sample_matches_the_renderer()
    {
        var expected = Docs.Normalize(File.ReadAllText(Docs.File("sample-report.md")));
        var actual = Docs.Normalize(
            MarkdownRenderer.Render(SampleData.Sessions(), Text.Create("en"), raw: false, SampleData.CapturedAt));

        Assert.Equal(expected, actual);
    }
}
