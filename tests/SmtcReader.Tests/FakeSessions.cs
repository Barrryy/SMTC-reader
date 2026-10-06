using SmtcReader.Smtc;

namespace SmtcReader.Tests;

/// <summary>Builds snapshots shaped like real ones, without needing a media session.</summary>
internal static class FakeSessions
{
    public static SessionSnapshot Spotify(
        string aumid = "SpotifyAB.SpotifyMusic_zpdnekdrzrea0!Spotify",
        string? appName = "Spotify",
        string? title = "Smells Like Teen Spirit",
        string? artist = "Nirvana",
        string status = "Playing",
        bool isCurrent = true) =>
        new()
        {
            SourceAppUserModelId = aumid,
            ResolvedAppName = appName,
            Playback = new PlaybackSnapshot
            {
                Status = status,
                Type = "Music",
                AutoRepeatMode = "None",
                IsShuffleActive = false,
                PlaybackRate = 1,
                Controls = new List<ControlState>
                {
                    new("IsPlayEnabled", false),
                    new("IsPauseEnabled", true),
                    new("IsNextEnabled", true),
                },
            },
            Timeline = new TimelineSnapshot
            {
                StartTime = TimeSpan.Zero,
                EndTime = TimeSpan.FromSeconds(301.975),
                MinSeekTime = TimeSpan.Zero,
                MaxSeekTime = TimeSpan.FromSeconds(301.975),
                Position = TimeSpan.FromSeconds(85.924),
                LastUpdatedTime = new DateTimeOffset(2026, 10, 6, 19, 1, 54, TimeSpan.FromHours(8)),
                LivePosition = TimeSpan.FromSeconds(86.247),
            },
            Media = new MediaPropertiesSnapshot
            {
                Title = title,
                Artist = artist,
                AlbumTitle = "Nevermind",
                TrackNumber = 1,
                AlbumTrackCount = 13,
                Genres = new[] { "Grunge" },
                PlaybackType = "Music",
                ThumbnailContentType = "image/png",
                ThumbnailSizeBytes = 2048,
            },
            IsCurrentSession = isCurrent,
            ThumbnailBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47 },
        };

    public static SessionSnapshot MinimalDesktopPlayer(string aumid = "cloudmusic.exe") =>
        new()
        {
            SourceAppUserModelId = aumid,
            ResolvedAppName = null,
            Playback = new PlaybackSnapshot { Status = "Playing", Type = "Music" },
            Timeline = new TimelineSnapshot
            {
                // An app that never pushes a timeline: everything zero, timestamp at 1601.
                LastUpdatedTime = new DateTimeOffset(1601, 1, 1, 8, 0, 0, TimeSpan.FromHours(8)),
            },
            Media = new MediaPropertiesSnapshot { Title = "pure imagination", Artist = "Rook1e" },
        };

    public static SessionSnapshot WithRaw(this SessionSnapshot session) =>
        session with
        {
            Raw = new[]
            {
                new RawBlock(
                    "PlaybackInfo",
                    new SortedDictionary<string, string>(StringComparer.Ordinal)
                    {
                        ["PlaybackStatus"] = "Playing",
                        ["IsShuffleActive"] = "<null>",
                        ["PlaybackRate"] = "1",
                    }),
            },
        };
}
