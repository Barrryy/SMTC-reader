using System.Globalization;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using SmtcReader.Cli;
using SmtcReader.Formatting;
using SmtcReader.Rendering;
using SmtcReader.Smtc;

namespace SmtcReader;

internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
        EnableUnicodeConsole();

        var parse = CliParser.Parse(args);
        var text = Text.Create(CliParser.FindLanguage(args));

        if (parse.Help)
        {
            Console.WriteLine(text.Help);
            return 0;
        }

        if (parse.Version)
        {
            Console.WriteLine("smtc-reader " + Version);
            return 0;
        }

        if (parse.Error is { Length: > 0 } error)
        {
            Console.Error.WriteLine(error);
            Console.Error.WriteLine("Try 'smtc-reader --help'.");
            return 2;
        }

        try
        {
            return await RunAsync(parse.Options!, text);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(text.Fatal(ex.Message));
            return 1;
        }
    }

    private static async Task<int> RunAsync(CliOptions options, Text text)
    {
        // With --json, stdout must stay machine readable, so status lines go to stderr.
        var status = options.Json ? Console.Error : Console.Out;
        var source = new WinRtSessionSource();
        var exportedThumbnails = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        string? reportPath = null;
        var reportAnnounced = false;
        var thumbnailNoticeShown = false;
        var iteration = 0;

        while (true)
        {
            iteration++;
            var capturedAt = DateTimeOffset.Now;
            var capture = await source.CaptureAsync(options.Raw);
            var sessions = SessionQuery.Apply(capture.Sessions, options.AppPattern, capture.CurrentSessionKey);

            if (options.Json)
            {
                Console.Out.WriteLine(JsonRenderer.Render(sessions, capturedAt));
            }
            else if (options.ListOnly)
            {
                ConsoleRenderer.RenderList(sessions, text, Console.Out);
            }
            else
            {
                ConsoleRenderer.RenderSessions(sessions, text, Console.Out, options.Raw);
            }

            if (options.ThumbnailPath is { Length: > 0 } thumbnailPath)
            {
                var written = ExportThumbnails(sessions, thumbnailPath, text, exportedThumbnails);
                if (!written && !thumbnailNoticeShown && sessions.Count == 1)
                {
                    status.WriteLine(text.ThumbnailMissing);
                    thumbnailNoticeShown = true;
                }
            }

            if (!options.NoMarkdown)
            {
                reportPath ??= BuildReportPath(options.OutDir, capturedAt);
                var markdown = MarkdownRenderer.Render(sessions, text, options.Raw, capturedAt);
                File.WriteAllText(reportPath, markdown, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
                if (!reportAnnounced)
                {
                    status.WriteLine(text.ReportWritten(reportPath));
                    reportAnnounced = true;
                }
            }

            if (!options.Watch)
            {
                break;
            }

            if (options.Count > 0 && iteration >= options.Count)
            {
                break;
            }

            await Task.Delay(TimeSpan.FromSeconds(Math.Max(0.05, options.Interval)));
        }

        return 0;
    }

    private static bool ExportThumbnails(
        IReadOnlyList<SessionSnapshot> sessions,
        string path,
        Text text,
        Dictionary<string, string> exported)
    {
        var wrote = false;

        for (var index = 0; index < sessions.Count; index++)
        {
            var session = sessions[index];
            if (session.ThumbnailBytes is not { Length: > 0 } bytes)
            {
                continue;
            }

            var target = sessions.Count > 1
                ? AddSessionSuffix(path, index + 1, session.SourceAppUserModelId)
                : path;
            target = ApplyContentTypeExtension(target, session.Media.ThumbnailContentType);

            var full = Path.GetFullPath(target);
            var fingerprint = Convert.ToHexString(SHA256.HashData(bytes));
            if (exported.TryGetValue(full, out var previous) && string.Equals(previous, fingerprint, StringComparison.Ordinal))
            {
                continue;
            }

            var directory = Path.GetDirectoryName(full);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllBytes(full, bytes);
            exported[full] = fingerprint;
            wrote = true;
            Console.WriteLine(text.ThumbnailSaved(full, session.Media.ThumbnailContentType));
        }

        return wrote;
    }

    private static string AddSessionSuffix(string path, int index, string aumid)
    {
        var directory = Path.GetDirectoryName(path) ?? string.Empty;
        var basename = Path.GetFileNameWithoutExtension(path);
        var extension = Path.GetExtension(path);
        return Path.Combine(directory, $"{basename}.{index}.{Sanitize(aumid)}{extension}");
    }

    private static string Sanitize(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var builder = new StringBuilder(value.Length);
        foreach (var character in value)
        {
            builder.Append(Array.IndexOf(invalid, character) >= 0 ? '_' : character);
        }

        return builder.ToString();
    }

    /// <summary>
    /// The extension is derived from the actual image bytes: some apps hand out BMP
    /// data under whatever name the caller suggested.
    /// </summary>
    private static string ApplyContentTypeExtension(string path, string? contentType)
    {
        if (string.IsNullOrEmpty(contentType) || contentType == ImageSniffer.UnknownContentType)
        {
            return path;
        }

        var wanted = ImageSniffer.ExtensionFor(contentType);
        return Path.GetExtension(path).Length > 0 ? Path.ChangeExtension(path, wanted) : path + wanted;
    }

    private static string BuildReportPath(string? outDir, DateTimeOffset capturedAt)
    {
        var directory = string.IsNullOrWhiteSpace(outDir) ? AppContext.BaseDirectory : outDir;
        Directory.CreateDirectory(directory);

        var stamp = capturedAt.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
        var path = Path.Combine(directory, $"smtc-{stamp}.md");
        var attempt = 1;
        while (File.Exists(path))
        {
            attempt++;
            path = Path.Combine(directory, $"smtc-{stamp}-{attempt}.md");
        }

        return path;
    }

    private static string Version
    {
        get
        {
            var assembly = typeof(Program).Assembly;
            var informational = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
            if (!string.IsNullOrEmpty(informational))
            {
                var plus = informational.IndexOf('+', StringComparison.Ordinal);
                return plus >= 0 ? informational[..plus] : informational;
            }

            return assembly.GetName().Version?.ToString() ?? "0.0.0";
        }
    }

    private static void EnableUnicodeConsole()
    {
        try
        {
            Console.OutputEncoding = Encoding.UTF8;
        }
        catch (IOException)
        {
            // Output is redirected to a pipe or file; the default encoding is fine.
        }
    }
}
