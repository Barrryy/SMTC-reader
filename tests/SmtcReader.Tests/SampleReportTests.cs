using System.Runtime.CompilerServices;
using SmtcReader.Rendering;
using SmtcReader.Smtc;
using Xunit;

namespace SmtcReader.Tests;

/// <summary>
/// Keeps docs/sample-report.md honest. The sample is hand written documentation, so
/// without this test it would silently drift away from what the renderer prints the
/// first time a column changes.
/// </summary>
public class SampleReportTests
{
    private static readonly DateTimeOffset CapturedAt =
        new(2026, 10, 6, 19, 4, 1, TimeSpan.FromHours(8));

    [Fact]
    public void The_documented_sample_matches_the_renderer()
    {
        var expected = Normalize(File.ReadAllText(SamplePath()));
        var actual = Normalize(MarkdownRenderer.Render(SampleSessions(), Text.Create("en"), raw: false, CapturedAt));

        Assert.Equal(expected, actual);
    }

    // Trailing newlines are a file-format detail, not content: the renderer ends with a
    // blank line, an editor saved file usually does not.
    private static string Normalize(string value) =>
        value.Replace("\r\n", "\n", StringComparison.Ordinal).TrimEnd('\n');

    private static string SamplePath([CallerFilePath] string thisFile = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(thisFile)!, "..", "..", "docs", "sample-report.md"));

    private static IReadOnlyList<SessionSnapshot> SampleSessions() =>
        new[]
        {
            new SessionSnapshot
            {
                SourceAppUserModelId = "SpotifyAB.SpotifyMusic_zpdnekdrzrea0!Spotify",
                ResolvedAppName = "Spotify",
                IsCurrentSession = true,
                Playback = new PlaybackSnapshot
                {
                    Status = "Playing",
                    Type = "Music",
                    AutoRepeatMode = "None",
                    IsShuffleActive = false,
                    PlaybackRate = 1,
                    Controls = AllControls(
                        "IsPlayEnabled",
                        "IsStopEnabled",
                        "IsFastForwardEnabled",
                        "IsRewindEnabled",
                        "IsPlaybackRateEnabled",
                        "IsChannelUpEnabled",
                        "IsChannelDownEnabled",
                        "IsRecordEnabled"),
                },
                Timeline = new TimelineSnapshot
                {
                    StartTime = TimeSpan.Zero,
                    EndTime = TimeSpan.FromSeconds(225),
                    MinSeekTime = TimeSpan.Zero,
                    MaxSeekTime = TimeSpan.FromSeconds(225),
                    Position = TimeSpan.FromSeconds(72.48),
                    LastUpdatedTime = new DateTimeOffset(2026, 10, 6, 19, 4, 0, 474, TimeSpan.FromHours(8)),
                    LivePosition = TimeSpan.FromSeconds(73.006),
                },
                Media = new MediaPropertiesSnapshot
                {
                    Title = "Example Song",
                    Artist = "Example Artist",
                    AlbumArtist = "Example Artist",
                    AlbumTitle = "Example Album",
                    TrackNumber = 3,
                    AlbumTrackCount = 10,
                    Genres = new[] { "Indie", "Dream Pop" },
                    PlaybackType = "Music",
                    ThumbnailContentType = "image/png",
                    ThumbnailSizeBytes = 151880,
                },
            },
            new SessionSnapshot
            {
                SourceAppUserModelId = "example-player.exe",
                ResolvedAppName = "Example Player",
                Playback = new PlaybackSnapshot
                {
                    Status = "Playing",
                    Type = "Music",
                    AutoRepeatMode = "List",
                    IsShuffleActive = true,
                    PlaybackRate = 1,
                    Controls = AllControls(
                        "IsPlayEnabled",
                        "IsFastForwardEnabled",
                        "IsRewindEnabled",
                        "IsChannelUpEnabled",
                        "IsChannelDownEnabled",
                        "IsRecordEnabled"),
                },
                Timeline = new TimelineSnapshot
                {
                    StartTime = TimeSpan.Zero,
                    EndTime = TimeSpan.FromSeconds(301.975),
                    MinSeekTime = TimeSpan.Zero,
                    MaxSeekTime = TimeSpan.FromSeconds(301.975),
                    Position = TimeSpan.FromSeconds(85.924),
                    LastUpdatedTime = new DateTimeOffset(2026, 10, 6, 19, 4, 0, 779, TimeSpan.FromHours(8)),
                    LivePosition = TimeSpan.FromSeconds(86.247),
                },
                Media = new MediaPropertiesSnapshot
                {
                    Title = "another-track.flac",
                    PlaybackType = "Music",
                    ThumbnailContentType = "image/bmp",
                    ThumbnailSizeBytes = 1638454,
                },
            },
        };

    private static IReadOnlyList<ControlState> AllControls(params string[] disabled) =>
        ControlCapabilities.All
            .Select(capability => new ControlState(
                capability.Property,
                !disabled.Contains(capability.Property, StringComparer.Ordinal)))
            .ToArray();
}
