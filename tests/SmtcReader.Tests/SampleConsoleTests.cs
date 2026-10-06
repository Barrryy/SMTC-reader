using SmtcReader.Rendering;
using Xunit;

namespace SmtcReader.Tests;

/// <summary>
/// Keeps docs/sample-console.txt — and therefore docs/screenshot.png, which is rendered
/// from it — honest. The image in the README has to be the real renderer output for the
/// example session, not a retouched mock-up.
/// </summary>
public class SampleConsoleTests
{
    [Fact]
    public void The_documented_console_sample_matches_the_renderer()
    {
        var writer = new StringWriter();
        ConsoleRenderer.RenderSessions(
            SampleData.Sessions().Take(1).ToArray(),
            Text.Create("en"),
            writer,
            raw: false);

        var expected = Docs.Normalize(File.ReadAllText(Docs.File("sample-console.txt")));
        var actual = Docs.Normalize(writer.ToString());

        Assert.Equal(expected, actual);
    }
}
