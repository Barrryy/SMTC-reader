using System.Globalization;
using System.Text;
using SmtcReader.Formatting;
using SmtcReader.Smtc;

namespace SmtcReader.Rendering;

/// <summary>Renders a report meant to be read by a human, in a Markdown viewer.</summary>
public static class MarkdownRenderer
{
    public static string Render(
        IReadOnlyList<SessionSnapshot> sessions,
        Text text,
        bool raw,
        DateTimeOffset capturedAt)
    {
        ArgumentNullException.ThrowIfNull(sessions);
        ArgumentNullException.ThrowIfNull(text);

        var builder = new StringBuilder();
        builder.AppendLine(text.ReportTitle);
        builder.AppendLine();
        builder.Append("- **").Append(text.CapturedAt).Append("**: ")
               .AppendLine(capturedAt.ToString("yyyy-MM-dd HH:mm:ss zzz", CultureInfo.InvariantCulture));
        builder.Append("- **").Append(text.SessionCount).Append("**: ")
               .AppendLine(sessions.Count.ToString(CultureInfo.InvariantCulture));
        builder.AppendLine();

        if (sessions.Count == 0)
        {
            builder.AppendLine(text.NoSessions);
            builder.AppendLine();
            return builder.ToString();
        }

        AppendOverview(builder, sessions, text);

        for (var index = 0; index < sessions.Count; index++)
        {
            AppendSession(builder, sessions[index], index + 1, text, raw);
        }

        builder.AppendLine("---");
        builder.AppendLine();
        builder.Append('_').Append(text.Footer).AppendLine("_");
        builder.AppendLine();
        return builder.ToString();
    }

    private static void AppendOverview(StringBuilder builder, IReadOnlyList<SessionSnapshot> sessions, Text text)
    {
        builder.Append("## ").AppendLine(text.Overview);
        builder.AppendLine();
        builder.Append("| # | ").Append(text.ColumnApp)
               .Append(" | ").Append(text.ColumnStatus)
               .Append(" | ").Append(text.ColumnTrack)
               .Append(" | ").Append(text.ColumnCurrent)
               .AppendLine(" |");
        builder.AppendLine("| :---: | :--- | :--- | :--- | :---: |");

        for (var index = 0; index < sessions.Count; index++)
        {
            var session = sessions[index];
            var app = session.ResolvedAppName is { Length: > 0 } name ? name : session.SourceAppUserModelId;
            var track = session.Media.Title ?? string.Empty;
            if (!string.IsNullOrEmpty(session.Media.Artist))
            {
                track = track.Length > 0 ? track + " — " + session.Media.Artist : session.Media.Artist!;
            }

            builder.Append("| ").Append(index + 1)
                   .Append(" | ").Append(Humanize.Cell(app))
                   .Append(" | ").Append(Humanize.Cell(session.Playback.Status))
                   .Append(" | ").Append(Humanize.Cell(track))
                   .Append(" | ").Append(session.IsCurrentSession ? "★" : string.Empty)
                   .AppendLine(" |");
        }

        builder.AppendLine();
    }

    private static void AppendSession(
        StringBuilder builder,
        SessionSnapshot session,
        int index,
        Text text,
        bool raw)
    {
        var app = session.ResolvedAppName is { Length: > 0 } name ? name : session.SourceAppUserModelId;
        var badge = session.IsCurrentSession ? " " + text.CurrentSessionBadge : string.Empty;

        builder.AppendLine("---");
        builder.AppendLine();
        builder.Append("## ").Append(index).Append(". ").Append(Humanize.Cell(app)).AppendLine(badge);
        builder.AppendLine();

        AppendDigest(builder, session, text);
        AppendBasicInfo(builder, session, text);
        AppendTimeline(builder, session, text);
        AppendMedia(builder, session, text);
        AppendControls(builder, session, text);

        if (session.MediaPropertiesError is { Length: > 0 } error)
        {
            builder.Append("> ⚠️ ").AppendLine(Humanize.Cell(text.MediaPropertiesFailed(error)));
            builder.AppendLine();
        }

        if (raw)
        {
            AppendRaw(builder, session, text);
        }
    }

    private static void AppendDigest(StringBuilder builder, SessionSnapshot session, Text text)
    {
        var parts = new List<string> { Humanize.Cell(session.Playback.Status) };
        if (session.Timeline.EndTime > TimeSpan.Zero)
        {
            var now = session.Timeline.LivePosition ?? session.Timeline.Position;
            parts.Add(Humanize.Duration(now) + " / " + Humanize.Duration(session.Timeline.EndTime));
        }

        if (session.Playback.PlaybackRate is { } rate && Math.Abs(rate - 1) > double.Epsilon)
        {
            parts.Add(rate.ToString("0.##", CultureInfo.InvariantCulture) + "x");
        }

        builder.Append("> **").Append(Humanize.Cell(session.Media.Title)).AppendLine("**  ");
        builder.Append("> ").AppendLine(string.Join(" · ", parts.Where(p => p.Length > 0)));
        builder.AppendLine();
        _ = text;
    }

    private static void AppendBasicInfo(StringBuilder builder, SessionSnapshot session, Text text)
    {
        builder.Append("### ").AppendLine(text.SectionBasic);
        builder.AppendLine();
        builder.Append("| ").Append(text.ColumnItem).Append(" | ").Append(text.ColumnValue).AppendLine(" |");
        builder.AppendLine("| :--- | :--- |");
        AppendRow(builder, text.FieldSourceApp, "`" + Humanize.Cell(session.SourceAppUserModelId) + "`");
        AppendRow(builder, text.FieldAppName, session.ResolvedAppName);
        AppendRow(builder, text.FieldPlaybackStatus, session.Playback.Status);
        AppendRow(builder, text.FieldPlaybackType, session.Playback.Type);
        AppendRow(builder, text.FieldAutoRepeat, session.Playback.AutoRepeatMode);
        AppendRow(builder, text.FieldShuffle, session.Playback.IsShuffleActive?.ToString());
        AppendRow(
            builder,
            text.FieldPlaybackRate,
            session.Playback.PlaybackRate?.ToString(CultureInfo.InvariantCulture));
        builder.AppendLine();
    }

    private static void AppendTimeline(StringBuilder builder, SessionSnapshot session, Text text)
    {
        var timeline = session.Timeline;
        builder.Append("### ").AppendLine(text.SectionTimeline);
        builder.AppendLine();
        builder.Append("| ").Append(text.ColumnItem).Append(" | ").Append(text.ColumnValue).AppendLine(" |");
        builder.AppendLine("| :--- | :--- |");
        AppendRow(builder, text.FieldStartTime, Humanize.Duration(timeline.StartTime));
        AppendRow(builder, text.FieldEndTime, Humanize.Duration(timeline.EndTime));
        AppendRow(
            builder,
            text.FieldSeekRange,
            Humanize.Duration(timeline.MinSeekTime) + " ~ " + Humanize.Duration(timeline.MaxSeekTime));
        AppendRow(builder, text.FieldReportedPosition, Humanize.Duration(timeline.Position));
        AppendRow(builder, text.FieldLivePosition, Humanize.Duration(timeline.LivePosition));
        AppendRow(builder, text.FieldUpdatedAt, Humanize.Timestamp(timeline.LastUpdatedTime));
        builder.AppendLine();
    }

    private static void AppendMedia(StringBuilder builder, SessionSnapshot session, Text text)
    {
        var media = session.Media;
        builder.Append("### ").AppendLine(text.SectionMedia);
        builder.AppendLine();
        builder.Append("| ").Append(text.ColumnItem).Append(" | ").Append(text.ColumnContent).AppendLine(" |");
        builder.AppendLine("| :--- | :--- |");
        AppendRow(builder, text.FieldTitle, media.Title);
        AppendRow(builder, text.FieldSubtitle, media.Subtitle);
        AppendRow(builder, text.FieldArtist, media.Artist);
        AppendRow(builder, text.FieldAlbumArtist, media.AlbumArtist);
        AppendRow(builder, text.FieldAlbumTitle, media.AlbumTitle);
        AppendRow(
            builder,
            text.FieldTrack,
            media.TrackNumber.ToString(CultureInfo.InvariantCulture) +
            " / " +
            media.AlbumTrackCount.ToString(CultureInfo.InvariantCulture));
        AppendRow(builder, text.FieldGenres, media.Genres.Count == 0 ? "<none>" : string.Join(" / ", media.Genres));
        AppendRow(
            builder,
            text.FieldThumbnail,
            media.ThumbnailSizeBytes is null
                ? null
                : media.ThumbnailContentType + " · " + Humanize.Size(media.ThumbnailSizeBytes));
        builder.AppendLine();
    }

    private static void AppendControls(StringBuilder builder, SessionSnapshot session, Text text)
    {
        builder.Append("### ").AppendLine(text.SectionControls);
        builder.AppendLine();

        if (session.Playback.Controls.Count == 0)
        {
            builder.Append('_').Append(text.ControlsNotProvided).AppendLine("_");
            builder.AppendLine();
            return;
        }

        builder.Append("| ").Append(text.ColumnAction)
               .Append(" | ").Append(text.ColumnProperty)
               .Append(" | ").Append(text.ColumnAvailable)
               .AppendLine(" |");
        builder.AppendLine("| :--- | :--- | :---: |");
        foreach (var control in session.Playback.Controls)
        {
            builder.Append("| ").Append(ControlCapabilities.Label(control.Property, text.IsChinese))
                   .Append(" | `").Append(control.Property)
                   .Append("` | ").Append(control.Enabled ? "✓" : "✗")
                   .AppendLine(" |");
        }

        builder.AppendLine();
    }

    private static void AppendRaw(StringBuilder builder, SessionSnapshot session, Text text)
    {
        if (session.Raw.Count == 0)
        {
            return;
        }

        builder.AppendLine("<details>");
        builder.Append("<summary>").Append(text.SectionRaw).AppendLine("</summary>");
        builder.AppendLine();
        foreach (var block in session.Raw)
        {
            if (block.Properties.Count == 0)
            {
                continue;
            }

            builder.Append("**").Append(block.Title).AppendLine("**");
            builder.AppendLine();
            foreach (var pair in block.Properties)
            {
                builder.Append("- `").Append(pair.Key).Append("` = ").AppendLine(Humanize.Cell(pair.Value));
            }

            builder.AppendLine();
        }

        builder.AppendLine("</details>");
        builder.AppendLine();
    }

    private static void AppendRow(StringBuilder builder, string label, string? value)
    {
        builder.Append("| ").Append(Humanize.Cell(label))
               .Append(" | ").Append(Humanize.Cell(value))
               .AppendLine(" |");
    }
}
