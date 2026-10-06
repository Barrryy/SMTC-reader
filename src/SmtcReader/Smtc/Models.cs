using System.Text.Json.Serialization;

namespace SmtcReader.Smtc;

/// <summary>One entry of the session's capability set.</summary>
public sealed record ControlState(string Property, bool Enabled);

/// <summary>Everything exposed through <c>GetPlaybackInfo()</c>.</summary>
public sealed record PlaybackSnapshot
{
    public string? Status { get; init; }

    public string? Type { get; init; }

    public string? AutoRepeatMode { get; init; }

    public bool? IsShuffleActive { get; init; }

    public double? PlaybackRate { get; init; }

    public IReadOnlyList<ControlState> Controls { get; init; } = Array.Empty<ControlState>();
}

/// <summary>Everything exposed through <c>GetTimelineProperties()</c>.</summary>
public sealed record TimelineSnapshot
{
    public TimeSpan StartTime { get; init; }

    public TimeSpan EndTime { get; init; }

    public TimeSpan MinSeekTime { get; init; }

    public TimeSpan MaxSeekTime { get; init; }

    public TimeSpan Position { get; init; }

    public DateTimeOffset LastUpdatedTime { get; init; }

    /// <summary>
    /// <see cref="Position"/> extrapolated to "now" using <see cref="LastUpdatedTime"/>,
    /// clamped to the seekable range. Null when the app never updates its timeline.
    /// </summary>
    public TimeSpan? LivePosition { get; init; }
}

/// <summary>Everything exposed through <c>TryGetMediaPropertiesAsync()</c>.</summary>
public sealed record MediaPropertiesSnapshot
{
    public string? Title { get; init; }

    public string? Subtitle { get; init; }

    public string? Artist { get; init; }

    public string? AlbumArtist { get; init; }

    public string? AlbumTitle { get; init; }

    public int TrackNumber { get; init; }

    public int AlbumTrackCount { get; init; }

    public IReadOnlyList<string> Genres { get; init; } = Array.Empty<string>();

    public string? PlaybackType { get; init; }

    public string? ThumbnailContentType { get; init; }

    public long? ThumbnailSizeBytes { get; init; }
}

/// <summary>A titled group of reflected property values, used by <c>--raw</c>.</summary>
public sealed record RawBlock(string Title, IReadOnlyDictionary<string, string> Properties);

/// <summary>A point-in-time copy of one SMTC session.</summary>
public sealed record SessionSnapshot
{
    public string SourceAppUserModelId { get; init; } = string.Empty;

    /// <summary>Best-effort friendly name; null when it cannot be resolved.</summary>
    public string? ResolvedAppName { get; init; }

    public PlaybackSnapshot Playback { get; init; } = new();

    public TimelineSnapshot Timeline { get; init; } = new();

    public MediaPropertiesSnapshot Media { get; init; } = new();

    /// <summary>True for the session Windows currently routes media keys to.</summary>
    public bool IsCurrentSession { get; init; }

    /// <summary>Set when <c>TryGetMediaPropertiesAsync()</c> failed for this session.</summary>
    public string? MediaPropertiesError { get; init; }

    public IReadOnlyList<RawBlock> Raw { get; init; } = Array.Empty<RawBlock>();

    [JsonIgnore]
    public byte[]? ThumbnailBytes { get; init; }
}

/// <summary>Result of one capture pass over all sessions.</summary>
public sealed record CaptureResult(IReadOnlyList<SessionSnapshot> Sessions, string? CurrentSessionKey);
