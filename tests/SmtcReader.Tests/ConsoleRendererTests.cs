using SmtcReader.Rendering;
using SmtcReader.Smtc;
using Xunit;

namespace SmtcReader.Tests;

public class ConsoleRendererTests
{
    [Fact]
    public void Aligns_every_capability_row_in_the_same_column()
    {
        var output = Render(FakeSessions.Spotify(), raw: false);
        var rows = output
            .Split('\n')
            .Select(line => line.TrimEnd('\r'))
            .Where(line => line.Contains("(Is", StringComparison.Ordinal))
            .ToArray();

        Assert.Equal(3, rows.Length);
        Assert.Single(rows.Select(row => row.IndexOf(" : ", StringComparison.Ordinal)).Distinct());
    }

    [Fact]
    public void Prints_the_identifiers_and_the_media_title()
    {
        var output = Render(FakeSessions.Spotify(), raw: false);

        Assert.Contains("SpotifyAB.SpotifyMusic_zpdnekdrzrea0!Spotify", output, StringComparison.Ordinal);
        Assert.Contains("Smells Like Teen Spirit", output, StringComparison.Ordinal);
        Assert.Contains("Nirvana", output, StringComparison.Ordinal);
        Assert.Contains("00:05:01.975", output, StringComparison.Ordinal);
    }

    [Fact]
    public void Marks_the_current_session()
    {
        Assert.Contains("current session", Render(FakeSessions.Spotify(), raw: false), StringComparison.Ordinal);
        Assert.DoesNotContain("current session", Render(FakeSessions.Spotify(isCurrent: false), raw: false), StringComparison.Ordinal);
    }

    [Fact]
    public void Keeps_null_and_empty_visible()
    {
        var output = Render(FakeSessions.MinimalDesktopPlayer() with { ResolvedAppName = null }, raw: false);

        Assert.Contains("<null>", output, StringComparison.Ordinal);
    }

    [Fact]
    public void Includes_raw_blocks_only_when_asked()
    {
        Assert.Contains("[PlaybackInfo]", Render(FakeSessions.Spotify().WithRaw(), raw: true), StringComparison.Ordinal);
        Assert.DoesNotContain("[PlaybackInfo]", Render(FakeSessions.Spotify().WithRaw(), raw: false), StringComparison.Ordinal);
    }

    [Fact]
    public void Reports_an_empty_capture()
    {
        var writer = new StringWriter();
        ConsoleRenderer.RenderSessions(Array.Empty<SessionSnapshot>(), Text.Create("en"), writer, raw: false);

        Assert.Contains("No matching SMTC sessions", writer.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void List_view_shows_status_app_and_track()
    {
        var writer = new StringWriter();
        ConsoleRenderer.RenderList(new[] { FakeSessions.Spotify() }, Text.Create("en"), writer);
        var output = writer.ToString();

        Assert.Contains("Playing", output, StringComparison.Ordinal);
        Assert.Contains("Spotify", output, StringComparison.Ordinal);
        Assert.Contains("Smells Like Teen Spirit — Nirvana", output, StringComparison.Ordinal);
    }

    [Fact]
    public void Chinese_output_uses_chinese_section_titles()
    {
        var writer = new StringWriter();
        ConsoleRenderer.RenderSessions(new[] { FakeSessions.Spotify() }, Text.Create("zh"), writer, raw: false);

        Assert.Contains("-- 可用操作 ", writer.ToString(), StringComparison.Ordinal);
    }

    private static string Render(SessionSnapshot session, bool raw)
    {
        var writer = new StringWriter();
        ConsoleRenderer.RenderSessions(new[] { session }, Text.Create("en"), writer, raw);
        return writer.ToString();
    }
}
