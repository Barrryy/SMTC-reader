using System.Globalization;

namespace SmtcReader;

/// <summary>
/// Every user visible string, in English and Simplified Chinese. Kept as plain
/// properties (rather than resource files) so the whole vocabulary of the tool can be
/// reviewed in one screen.
/// </summary>
public sealed class Text
{
    private readonly bool _zh;

    private Text(bool zh) => _zh = zh;

    public bool IsChinese => _zh;

    public static Text Create(string? language)
    {
        if (string.IsNullOrEmpty(language) ||
            string.Equals(language, "auto", StringComparison.OrdinalIgnoreCase))
        {
            var ui = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            return new Text(string.Equals(ui, "zh", StringComparison.OrdinalIgnoreCase));
        }

        var normalized = language.ToLowerInvariant();
        return new Text(normalized.StartsWith("zh", StringComparison.Ordinal));
    }

    // ---- sections ---------------------------------------------------------
    public string Overview => _zh ? "会话一览" : "Sessions";

    public string SectionBasic => _zh ? "基本信息" : "Basic info";

    public string SectionPlayback => _zh ? "播放" : "Playback";

    public string SectionControls => _zh ? "可用操作" : "Controls";

    public string SectionTimeline => _zh ? "时间轴" : "Timeline";

    public string SectionMedia => _zh ? "媒体信息" : "Media properties";

    public string SectionRaw => _zh ? "原始属性（反射枚举）" : "Raw properties (reflection)";

    // ---- report preamble --------------------------------------------------
    public string ReportTitle => "# SMTC Reader " + (_zh ? "会话快照" : "session snapshot");

    public string CapturedAt => _zh ? "采集时间" : "Captured at";

    public string SessionCount => _zh ? "会话数量" : "Sessions";

    // ---- table headers ----------------------------------------------------
    public string ColumnApp => _zh ? "应用" : "App";

    public string ColumnStatus => _zh ? "状态" : "Status";

    public string ColumnTrack => _zh ? "曲目" : "Track";

    public string ColumnCurrent => _zh ? "当前" : "Current";

    public string ColumnItem => _zh ? "项目" : "Item";

    public string ColumnValue => _zh ? "值" : "Value";

    public string ColumnContent => _zh ? "内容" : "Content";

    public string ColumnAction => _zh ? "操作" : "Action";

    public string ColumnProperty => _zh ? "属性" : "Property";

    public string ColumnAvailable => _zh ? "可用" : "Available";

    // ---- fields -----------------------------------------------------------
    public string FieldSourceApp => _zh ? "来源应用 (AUMID)" : "Source app (AUMID)";

    public string FieldAppName => _zh ? "应用显示名" : "Resolved app name";

    public string FieldPlaybackStatus => _zh ? "播放状态" : "Playback status";

    public string FieldPlaybackType => _zh ? "媒体类型" : "Media type";

    public string FieldAutoRepeat => _zh ? "循环模式" : "Repeat mode";

    public string FieldShuffle => _zh ? "随机播放" : "Shuffle";

    public string FieldPlaybackRate => _zh ? "倍速" : "Playback rate";

    public string FieldStartTime => _zh ? "起点" : "Start";

    public string FieldEndTime => _zh ? "终点" : "End";

    public string FieldSeekRange => _zh ? "可拖动范围" : "Seek range";

    public string FieldReportedPosition => _zh ? "应用上报进度" : "Reported position";

    public string FieldLivePosition => _zh ? "按上报时间外推" : "Extrapolated position";

    public string FieldUpdatedAt => _zh ? "进度更新时间" : "Position updated at";

    public string FieldTitle => _zh ? "曲名" : "Title";

    public string FieldSubtitle => _zh ? "副标题" : "Subtitle";

    public string FieldArtist => _zh ? "歌手" : "Artist";

    public string FieldAlbumArtist => _zh ? "专辑歌手" : "Album artist";

    public string FieldAlbumTitle => _zh ? "专辑" : "Album";

    public string FieldTrack => _zh ? "音轨" : "Track";

    public string FieldGenres => _zh ? "流派" : "Genres";

    public string FieldThumbnail => _zh ? "封面" : "Thumbnail";

    // ---- messages ---------------------------------------------------------
    public string CurrentSessionBadge => _zh ? "★ 当前会话" : "★ current session";

    public string NoSessions =>
        _zh
            ? "没有检测到 SMTC 会话。放点音乐/视频，或者确认应用支持 SMTC。"
            : "No SMTC sessions found. Play something, or make sure the app supports SMTC.";

    public string NoMatchingSessions => _zh ? "没有匹配的 SMTC 会话。" : "No matching SMTC sessions.";

    public string ControlsNotProvided =>
        _zh ? "该应用没有提供 Controls 信息。" : "This app does not expose a capability set.";

    public string MediaPropertiesFailed(string reason) =>
        _zh ? $"取媒体属性失败：{reason}" : $"Could not read media properties: {reason}";

    public string ReportWritten(string path) =>
        _zh ? $"Markdown 报告: {path}" : $"Markdown report: {path}";

    public string ThumbnailSaved(string path, string? contentType) =>
        _zh ? $"封面已保存: {path}  [{contentType}]" : $"Thumbnail saved: {path}  [{contentType}]";

    public string ThumbnailMissing =>
        _zh ? "该会话没有可导出的封面。" : "This session has no thumbnail to export.";

    public string Hint =>
        _zh
            ? "提示: --raw 列出全部属性, --json 供程序读取, --watch 持续刷新。"
            : "Tip: --raw for every property, --json for scripts, --watch to refresh.";

    public string Fatal(string reason) => _zh ? $"出错: {reason}" : $"Error: {reason}";

    public string Footer =>
        _zh
            ? "由 smtc-reader 生成。所有字段均来自 Windows SMTC (System Media Transport Controls) 公开 API，字段是否填充取决于应用本身。"
            : "Generated by smtc-reader. Every field comes from the public Windows SMTC (System Media Transport Controls) API; whether an app populates them is up to the app.";

    public string Help => _zh ? HelpZh : HelpEn;

    private const string HelpEn = """
        smtc-reader - dump everything a Windows media session exposes

        USAGE
          smtc-reader [options]

        OPTIONS
          -a, --app <pattern>        Only sessions whose AUMID or resolved app name matches.
                                     Supports * wildcards; without one it is a substring match.
          -l, --list                 Print the overview table only.
          -w, --watch                Keep refreshing until Ctrl+C.
          -i, --interval <seconds>   Refresh interval for --watch (default 1.0).
          -n, --count <n>            Stop after n refreshes; 0 means run until Ctrl+C.
          -t, --thumbnail <path>     Write the album art to this path.
              --out-dir <dir>        Where to write the Markdown report (default: next to the exe).
              --no-markdown          Do not write a Markdown report.
              --raw                  Also dump every reflected property of every object.
          -j, --json                 Emit JSON instead of the text report.
              --lang <auto|en|zh>    Output language (default: auto).
          -h, --help                 Show this help.
              --version              Show the version.

        EXIT CODES
          0  success (even when no session is present)
          1  runtime failure
          2  bad command line

        A Markdown report is written next to the executable on every run unless
        --no-markdown is given. In --watch mode the same file is updated instead of
        creating one per refresh.
        """;

    private const string HelpZh = """
        smtc-reader - 把 Windows 媒体会话抛出来的信息全部打出来

        用法
          smtc-reader [选项]

        选项
          -a, --app <关键词>         只保留 AUMID 或应用显示名匹配的会话。
                                     支持 * 通配符；不含通配符时按子串匹配。
          -l, --list                 只打印概览表。
          -w, --watch                持续刷新，Ctrl+C 退出。
          -i, --interval <秒>        --watch 的刷新间隔（默认 1.0）。
          -n, --count <次数>         刷够 n 次就停；0 表示一直刷到 Ctrl+C。
          -t, --thumbnail <路径>     把封面写到这个路径。
              --out-dir <目录>       Markdown 报告的输出目录（默认：exe 旁边）。
              --no-markdown          不写 Markdown 报告。
              --raw                  额外反射列出每个对象的所有属性。
          -j, --json                 输出 JSON，而不是文本报告。
              --lang <auto|en|zh>    输出语言（默认 auto，跟随系统）。
          -h, --help                 显示帮助。
              --version              显示版本。

        退出码
          0  成功（即使没有检测到会话）
          1  运行失败
          2  命令行参数错误

        每次运行都会在 exe 旁边写一份 Markdown 报告，除非指定 --no-markdown。
        --watch 模式下只更新同一份文件，不会每次刷新都新建。
        """;
}
