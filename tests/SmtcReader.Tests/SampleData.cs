using SmtcReader.Smtc;

namespace SmtcReader.Tests;

/// <summary>
/// The illustrative session set behind the documentation: docs/sample-report.md and
/// docs/sample-console.txt. Both are asserted against what the renderers really print,
/// so the documentation cannot drift away from the tool.
/// </summary>
internal static class SampleData
{
    public static readonly DateTimeOffset CapturedAt =
        new(2026, 10, 6, 19, 4, 1, TimeSpan.FromHours(8));

    /// <summary>
    /// Two ends of the range: an app that reports everything, and a desktop player that
    /// reports almost nothing but a file name.
    /// </summary>
    public static IReadOnlyList<SessionSnapshot> Sessions() =>
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
