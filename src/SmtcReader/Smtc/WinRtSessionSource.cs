using System.ComponentModel;
using System.Globalization;
using System.Diagnostics;
using Windows.Media.Control;
using Windows.Storage.Streams;

namespace SmtcReader.Smtc;

/// <summary>Reads live sessions through the Windows SMTC API.</summary>
public sealed class WinRtSessionSource : ISessionSource
{
    private const int MaxThumbnailBytes = 16 * 1024 * 1024;

    private readonly Dictionary<string, string?> _appNameCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly Lock _cacheLock = new();

    public async Task<CaptureResult> CaptureAsync(bool includeRaw)
    {
        var manager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();

        var sessions = new List<SessionSnapshot>();
        foreach (var session in manager.GetSessions())
        {
            sessions.Add(await CaptureSessionAsync(session, includeRaw));
        }

        string? currentKey = null;
        var current = manager.GetCurrentSession();
        if (current is not null)
        {
            currentKey = SessionQuery.Key(current.SourceAppUserModelId, await TryReadTitleAsync(current));
        }

        return new CaptureResult(sessions, currentKey);
    }

    private async Task<SessionSnapshot> CaptureSessionAsync(
        GlobalSystemMediaTransportControlsSession session,
        bool includeRaw)
    {
        var aumid = session.SourceAppUserModelId;
        var playback = session.GetPlaybackInfo();
        var timeline = session.GetTimelineProperties();

        GlobalSystemMediaTransportControlsSessionMediaProperties? media = null;
        string? mediaError = null;
        try
        {
            media = await session.TryGetMediaPropertiesAsync();
        }
        catch (Exception ex)
        {
            mediaError = ex.Message;
        }

        var thumbnail = await TryReadThumbnailAsync(media?.Thumbnail);

        return new SessionSnapshot
        {
            SourceAppUserModelId = aumid,
            ResolvedAppName = ResolveAppName(aumid),
            Playback = new PlaybackSnapshot
            {
                Status = playback.PlaybackStatus.ToString(),
                Type = playback.PlaybackType?.ToString(),
                AutoRepeatMode = playback.AutoRepeatMode?.ToString(),
                IsShuffleActive = playback.IsShuffleActive,
                PlaybackRate = playback.PlaybackRate,
                Controls = ControlCapabilities.Read(playback.Controls),
            },
            Timeline = new TimelineSnapshot
            {
                StartTime = timeline.StartTime,
                EndTime = timeline.EndTime,
                MinSeekTime = timeline.MinSeekTime,
                MaxSeekTime = timeline.MaxSeekTime,
                Position = timeline.Position,
                LastUpdatedTime = timeline.LastUpdatedTime,
                LivePosition = ExtrapolatePosition(playback.PlaybackStatus, timeline),
            },
            Media = new MediaPropertiesSnapshot
            {
                Title = media?.Title,
                Subtitle = media?.Subtitle,
                Artist = media?.Artist,
                AlbumArtist = media?.AlbumArtist,
                AlbumTitle = media?.AlbumTitle,
                TrackNumber = media?.TrackNumber ?? 0,
                AlbumTrackCount = media?.AlbumTrackCount ?? 0,
                Genres = media?.Genres?.ToArray() ?? Array.Empty<string>(),
                PlaybackType = media?.PlaybackType?.ToString(),
                ThumbnailContentType = thumbnail.ContentType,
                ThumbnailSizeBytes = thumbnail.Bytes?.LongLength,
            },
            MediaPropertiesError = mediaError,
            Raw = includeRaw ? CaptureRaw(session, playback, timeline, media) : Array.Empty<RawBlock>(),
            ThumbnailBytes = thumbnail.Bytes,
        };
    }

    private static async Task<string?> TryReadTitleAsync(GlobalSystemMediaTransportControlsSession session)
    {
        try
        {
            return (await session.TryGetMediaPropertiesAsync())?.Title;
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>
    /// <c>Position</c> is a snapshot taken when the app last pushed an update, so a
    /// playing session needs it advanced by the time since <c>LastUpdatedTime</c>.
    /// Several apps never update that timestamp (they report 1601-01-01), and some
    /// report a stopwatch well past the media length - both are rejected here.
    /// </summary>
    private static TimeSpan? ExtrapolatePosition(
        GlobalSystemMediaTransportControlsSessionPlaybackStatus status,
        GlobalSystemMediaTransportControlsSessionTimelineProperties timeline)
    {
        if (status != GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing)
        {
            return null;
        }

        var updated = timeline.LastUpdatedTime;
        if (updated.Year < 2000)
        {
            return null;
        }

        var elapsed = DateTimeOffset.Now - updated;
        if (elapsed <= TimeSpan.Zero || elapsed > TimeSpan.FromHours(24))
        {
            return null;
        }

        var candidate = timeline.Position + elapsed;
        var upperBound = timeline.EndTime > TimeSpan.Zero ? timeline.EndTime : timeline.MaxSeekTime;
        if (upperBound > TimeSpan.Zero && candidate > upperBound)
        {
            return upperBound;
        }

        return candidate;
    }

    private static async Task<(byte[]? Bytes, string? ContentType)> TryReadThumbnailAsync(
        IRandomAccessStreamReference? reference)
    {
        if (reference is null)
        {
            return (null, null);
        }

        try
        {
            using var stream = await reference.OpenReadAsync();
            var size = stream.Size;
            if (size == 0 || size > MaxThumbnailBytes)
            {
                return (null, null);
            }

            // A DataReader is used instead of AsStreamForRead because the projection
            // does not expose the stream extension methods on every SDK version.
            using var reader = new DataReader(stream.GetInputStreamAt(0));
            await reader.LoadAsync((uint)size);
            var bytes = new byte[(int)size];
            reader.ReadBytes(bytes);
            return (bytes, ImageSniffer.DetectContentType(bytes));
        }
        catch (Exception)
        {
            return (null, null);
        }
    }

    private static IReadOnlyList<RawBlock> CaptureRaw(
        GlobalSystemMediaTransportControlsSession session,
        GlobalSystemMediaTransportControlsSessionPlaybackInfo playback,
        GlobalSystemMediaTransportControlsSessionTimelineProperties timeline,
        GlobalSystemMediaTransportControlsSessionMediaProperties? media)
    {
        var blocks = new List<RawBlock>
        {
            new("Session", Reflect(session)),
            new("PlaybackInfo", Reflect(playback)),
            new("TimelineProperties", Reflect(timeline)),
        };

        if (media is not null)
        {
            blocks.Add(new("MediaProperties", Reflect(media)));
            if (media.Thumbnail is not null)
            {
                blocks.Add(new("Thumbnail (RandomAccessStreamReference)", Reflect(media.Thumbnail)));
            }
        }

        return blocks;
    }

    /// <summary>
    /// Reflecting over the projection types (rather than hard coding a field list) is
    /// what keeps <c>--raw</c> complete when a Windows update adds properties.
    /// </summary>
    private static IReadOnlyDictionary<string, string> Reflect(object? value)
    {
        var result = new SortedDictionary<string, string>(StringComparer.Ordinal);
        if (value is null)
        {
            return result;
        }

        foreach (var property in value.GetType().GetProperties())
        {
            string text;
            try
            {
                text = Describe(property.GetValue(value));
            }
            catch (Exception ex)
            {
                text = "<" + ex.Message + ">";
            }

            result[property.Name] = text;
        }

        return result;
    }

    private static string Describe(object? value) => value switch
    {
        null => "<null>",
        string s when s.Length == 0 => "<empty>",
        string s => s,
        DateTimeOffset timestamp => timestamp.ToString("o", CultureInfo.InvariantCulture),
        TimeSpan duration => duration.ToString("c", CultureInfo.InvariantCulture),
        System.Collections.IEnumerable sequence => DescribeSequence(sequence),
        _ => value.ToString() ?? "<null>",
    };

    /// <summary>
    /// WinRT collections project to CLR types whose ToString() is a type name, so they
    /// are flattened instead. This is what turns an empty Genres list into "&lt;none&gt;".
    /// </summary>
    private static string DescribeSequence(System.Collections.IEnumerable sequence)
    {
        var items = new List<string>();
        foreach (var item in sequence)
        {
            items.Add(item?.ToString() ?? "<null>");
        }

        return items.Count == 0 ? "<none>" : string.Join(", ", items);
    }

    private string? ResolveAppName(string aumid)
    {
        if (string.IsNullOrWhiteSpace(aumid))
        {
            return null;
        }

        lock (_cacheLock)
        {
            if (_appNameCache.TryGetValue(aumid, out var cached))
            {
                return cached;
            }
        }

        var name = LookupProcessDescription(aumid);

        lock (_cacheLock)
        {
            _appNameCache[aumid] = name;
        }

        return name;
    }

    /// <summary>
    /// Best-effort friendly name. Desktop players report their executable name as the
    /// AUMID and are running (they are producing media, after all), so the executable's
    /// version description is a good name source. Packaged apps are left as-is.
    /// </summary>
    private static string? LookupProcessDescription(string aumid)
    {
        var executable = ExtractExecutableName(aumid);
        if (executable is null)
        {
            return null;
        }

        var processName = Path.GetFileNameWithoutExtension(executable);
        if (string.IsNullOrEmpty(processName))
        {
            return null;
        }

        foreach (var process in Process.GetProcessesByName(processName))
        {
            try
            {
                var description = process.MainModule?.FileVersionInfo?.FileDescription;
                if (!string.IsNullOrWhiteSpace(description))
                {
                    return description;
                }
            }
            catch (Win32Exception)
            {
                // Different bitness or access denied; try the next match.
            }
            catch (InvalidOperationException)
            {
                // Process exited between enumeration and inspection.
            }
            finally
            {
                process.Dispose();
            }
        }

        return null;
    }

    private static string? ExtractExecutableName(string aumid)
    {
        var leaf = aumid;
        var bang = leaf.IndexOf('!', StringComparison.Ordinal);
        if (bang >= 0)
        {
            leaf = leaf[..bang];
        }

        var slash = leaf.LastIndexOf('\\');
        if (slash >= 0)
        {
            leaf = leaf[(slash + 1)..];
        }

        return leaf.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ? leaf : null;
    }
}
