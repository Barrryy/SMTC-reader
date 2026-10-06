using SmtcReader.Formatting;
using SmtcReader.Smtc;

namespace SmtcReader.Rendering;

/// <summary>Human readable console output. Pure: takes the snapshot set and a writer.</summary>
public static class ConsoleRenderer
{
    private const int Width = 78;
    private const int MaxLabelWidth = 44;

    public static void RenderSessions(
        IReadOnlyList<SessionSnapshot> sessions,
        Text text,
        TextWriter output,
        bool raw)
    {
        ArgumentNullException.ThrowIfNull(output);

        if (sessions.Count == 0)
        {
            output.WriteLine(text.NoMatchingSessions);
            return;
        }

        for (var index = 0; index < sessions.Count; index++)
        {
            RenderSession(sessions[index], index + 1, sessions.Count, text, output, raw);
        }

        output.WriteLine();
        output.WriteLine(text.Hint);
    }

    public static void RenderList(IReadOnlyList<SessionSnapshot> sessions, Text text, TextWriter output)
    {
        ArgumentNullException.ThrowIfNull(output);

        if (sessions.Count == 0)
        {
            output.WriteLine(text.NoMatchingSessions);
            return;
        }

        output.WriteLine();
        output.WriteLine(
            "  " + TextLayout.Pad("#", 3) +
            TextLayout.Pad(text.ColumnStatus, 12) +
            TextLayout.Pad(text.ColumnApp, 26) +
            text.ColumnTrack);
        output.WriteLine(new string('-', Width));

        for (var index = 0; index < sessions.Count; index++)
        {
            var session = sessions[index];
            var app = session.ResolvedAppName is { Length: > 0 } name ? name : session.SourceAppUserModelId;
            var track = session.Media.Title ?? string.Empty;
            if (!string.IsNullOrEmpty(session.Media.Artist))
            {
                track = track.Length > 0 ? track + " — " + session.Media.Artist : session.Media.Artist!;
            }

            var marker = session.IsCurrentSession ? "  *" : string.Empty;
            output.WriteLine(
                "  " + TextLayout.Pad((index + 1).ToString(System.Globalization.CultureInfo.InvariantCulture), 3) +
                TextLayout.Pad(session.Playback.Status ?? string.Empty, 12) +
                TextLayout.Pad(TextLayout.Truncate(app, 24), 26) +
                TextLayout.Truncate(track, 40) + marker);
        }

        output.WriteLine();
    }

    private static void RenderSession(
        SessionSnapshot session,
        int index,
        int total,
        Text text,
        TextWriter output,
        bool raw)
    {
        var appName = session.ResolvedAppName is { Length: > 0 } name ? $"  ({name})" : string.Empty;
        var current = session.IsCurrentSession ? "  <-- " + (text.IsChinese ? "当前会话" : "current session") : string.Empty;

        output.WriteLine();
        output.WriteLine(new string('=', Width));
        output.WriteLine($" [{index}/{total}] {session.SourceAppUserModelId}{appName}{current}");
        output.WriteLine(new string('=', Width));
        WriteFields(output, new[]
        {
            (text.FieldSourceApp, session.SourceAppUserModelId),
            (text.FieldAppName, Humanize.Console(session.ResolvedAppName)),
        });

        output.WriteLine();
        WriteSection(output, text.SectionPlayback);
        WriteFields(output, new[]
        {
            (text.FieldPlaybackStatus, Humanize.Console(session.Playback.Status)),
            (text.FieldPlaybackType, Humanize.Console(session.Playback.Type)),
            (text.FieldAutoRepeat, Humanize.Console(session.Playback.AutoRepeatMode)),
            (text.FieldShuffle, Humanize.Console(session.Playback.IsShuffleActive?.ToString())),
            (text.FieldPlaybackRate, Humanize.Console(session.Playback.PlaybackRate?.ToString(System.Globalization.CultureInfo.InvariantCulture))),
        });

        output.WriteLine();
        WriteSection(output, text.SectionControls);
        if (session.Playback.Controls.Count == 0)
        {
            output.WriteLine("  " + text.ControlsNotProvided);
        }
        else
        {
            var controls = session.Playback.Controls
                .Select(control => (
                    Label: $"{ControlCapabilities.Label(control.Property, text.IsChinese)} ({control.Property})",
                    Value: control.Enabled ? "✓" : "✗"))
                .ToArray();
            WriteFields(output, controls);
        }

        output.WriteLine();
        WriteSection(output, text.SectionTimeline);
        WriteFields(output, new[]
        {
            (text.FieldStartTime, Humanize.Duration(session.Timeline.StartTime)),
            (text.FieldEndTime, Humanize.Duration(session.Timeline.EndTime)),
            (text.FieldSeekRange, Humanize.Duration(session.Timeline.MinSeekTime) + " ~ " + Humanize.Duration(session.Timeline.MaxSeekTime)),
            (text.FieldReportedPosition, Humanize.Duration(session.Timeline.Position)),
            (text.FieldLivePosition, Humanize.Duration(session.Timeline.LivePosition)),
            (text.FieldUpdatedAt, Humanize.Timestamp(session.Timeline.LastUpdatedTime)),
        });

        output.WriteLine();
        WriteSection(output, text.SectionMedia);
        WriteFields(output, new[]
        {
            (text.FieldTitle, Humanize.Console(session.Media.Title)),
            (text.FieldSubtitle, Humanize.Console(session.Media.Subtitle)),
            (text.FieldArtist, Humanize.Console(session.Media.Artist)),
            (text.FieldAlbumArtist, Humanize.Console(session.Media.AlbumArtist)),
            (text.FieldAlbumTitle, Humanize.Console(session.Media.AlbumTitle)),
            (text.FieldTrack, session.Media.TrackNumber.ToString(System.Globalization.CultureInfo.InvariantCulture) +
                " / " +
                session.Media.AlbumTrackCount.ToString(System.Globalization.CultureInfo.InvariantCulture)),
            (text.FieldGenres, session.Media.Genres.Count == 0 ? "<none>" : string.Join(", ", session.Media.Genres)),
            (text.FieldThumbnail, session.Media.ThumbnailSizeBytes is null
                ? Humanize.Console(null)
                : $"{session.Media.ThumbnailContentType} / {Humanize.Size(session.Media.ThumbnailSizeBytes)}"),
        });

        if (session.MediaPropertiesError is { Length: > 0 } error)
        {
            output.WriteLine();
            output.WriteLine("  " + text.MediaPropertiesFailed(error));
        }

        if (raw && session.Raw.Count > 0)
        {
            output.WriteLine();
            WriteSection(output, text.SectionRaw);
            foreach (var block in session.Raw)
            {
                output.WriteLine($"  [{block.Title}]");
                foreach (var pair in block.Properties)
                {
                    output.WriteLine("    " + TextLayout.Pad(pair.Key, 22) + ": " + pair.Value);
                }
            }
        }
    }

    private static void WriteSection(TextWriter output, string title)
    {
        var header = "-- " + title + " ";
        var padding = Math.Max(0, Width - TextLayout.DisplayWidth(header));
        output.WriteLine(header + new string('-', padding));
    }

    /// <summary>
    /// Aligns a section's values on their own column, so long labels (a capability name
    /// plus its API property) do not push the values around.
    /// </summary>
    private static void WriteFields(TextWriter output, IReadOnlyList<(string Label, string Value)> fields)
    {
        var width = 0;
        foreach (var field in fields)
        {
            width = Math.Max(width, TextLayout.DisplayWidth(field.Label));
        }

        width = Math.Min(width, MaxLabelWidth);
        foreach (var field in fields)
        {
            output.WriteLine("  " + TextLayout.Pad(field.Label, width) + " : " + field.Value);
        }
    }
}
