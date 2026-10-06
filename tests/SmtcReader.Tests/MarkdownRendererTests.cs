using SmtcReader.Rendering;
using SmtcReader.Smtc;
using Xunit;

namespace SmtcReader.Tests;

public class MarkdownRendererTests
{
    private static readonly DateTimeOffset CapturedAt =
        new(2026, 10, 6, 19, 2, 16, TimeSpan.FromHours(8));

    [Fact]
    public void Renders_a_header_an_overview_row_and_the_field_tables()
    {
        var markdown = MarkdownRenderer.Render(new[] { FakeSessions.Spotify() }, Text.Create("en"), false, CapturedAt);

        Assert.Contains("# SMTC Reader", markdown, StringComparison.Ordinal);
        Assert.Contains("- **Captured at**: 2026-10-06 19:02:16 +08:00", markdown, StringComparison.Ordinal);
        Assert.Contains("| 1 | Spotify | Playing | Smells Like Teen Spirit — Nirvana | ★ |", markdown, StringComparison.Ordinal);
        Assert.Contains("| Source app (AUMID) | `SpotifyAB.SpotifyMusic_zpdnekdrzrea0!Spotify` |", markdown, StringComparison.Ordinal);
        Assert.Contains("| Reported position | 00:01:25.924 |", markdown, StringComparison.Ordinal);
        Assert.Contains("| Thumbnail | image/png · 2.00 KB |", markdown, StringComparison.Ordinal);
    }

    [Fact]
    public void Renders_the_capability_table()
    {
        var markdown = MarkdownRenderer.Render(new[] { FakeSessions.Spotify() }, Text.Create("en"), false, CapturedAt);

        Assert.Contains("| Play | `IsPlayEnabled` | ✗ |", markdown, StringComparison.Ordinal);
        Assert.Contains("| Pause | `IsPauseEnabled` | ✓ |", markdown, StringComparison.Ordinal);
    }

    [Fact]
    public void Shows_a_dash_for_values_the_app_never_reports()
    {
        var markdown = MarkdownRenderer.Render(
            new[] { FakeSessions.MinimalDesktopPlayer() },
            Text.Create("en"),
            false,
            CapturedAt);

        Assert.Contains("| Album | — |", markdown, StringComparison.Ordinal);
        Assert.Contains("| Extrapolated position | — |", markdown, StringComparison.Ordinal);
        Assert.Contains("| Position updated at | — |", markdown, StringComparison.Ordinal);
        Assert.Contains("This app does not expose a capability set.", markdown, StringComparison.Ordinal);
    }

    [Fact]
    public void Raw_properties_are_collapsed_into_a_details_block()
    {
        var markdown = MarkdownRenderer.Render(
            new[] { FakeSessions.Spotify().WithRaw() },
            Text.Create("en"),
            true,
            CapturedAt);

        Assert.Contains("<details>", markdown, StringComparison.Ordinal);
        Assert.Contains("- `PlaybackStatus` = Playing", markdown, StringComparison.Ordinal);
        Assert.Contains("</details>", markdown, StringComparison.Ordinal);
    }

    [Fact]
    public void Raw_properties_are_absent_unless_requested()
    {
        var markdown = MarkdownRenderer.Render(
            new[] { FakeSessions.Spotify().WithRaw() },
            Text.Create("en"),
            false,
            CapturedAt);

        Assert.DoesNotContain("<details>", markdown, StringComparison.Ordinal);
    }

    [Fact]
    public void Chinese_output_uses_chinese_labels()
    {
        var markdown = MarkdownRenderer.Render(new[] { FakeSessions.Spotify() }, Text.Create("zh"), false, CapturedAt);

        Assert.Contains("### 可用操作", markdown, StringComparison.Ordinal);
        Assert.Contains("| 播放 | `IsPlayEnabled` | ✗ |", markdown, StringComparison.Ordinal);
        Assert.Contains("会话数量", markdown, StringComparison.Ordinal);
    }

    [Fact]
    public void An_empty_capture_still_produces_a_report()
    {
        var markdown = MarkdownRenderer.Render(Array.Empty<SessionSnapshot>(), Text.Create("en"), false, CapturedAt);

        Assert.Contains("No SMTC sessions found", markdown, StringComparison.Ordinal);
        Assert.DoesNotContain("## 1.", markdown, StringComparison.Ordinal);
    }
}
