using System.Runtime.CompilerServices;

namespace SmtcReader.Tests;

/// <summary>Locates a documentation file from a test, independent of the build output path.</summary>
internal static class Docs
{
    public static string File(string name, [CallerFilePath] string thisFile = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(thisFile)!, "..", "..", "docs", name));

    /// <summary>
    /// Line endings are a file-format detail, not content: the renderers emit CRLF on
    /// Windows while the committed files use LF.
    /// </summary>
    public static string Normalize(string value) =>
        value.Replace("\r\n", "\n", StringComparison.Ordinal).TrimEnd('\n');
}
