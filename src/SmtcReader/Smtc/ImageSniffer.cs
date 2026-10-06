namespace SmtcReader.Smtc;

/// <summary>
/// Identifies an image from its magic bytes. Necessary because a stream's
/// <c>ContentType</c> is not reliably populated across media apps, and some of them
/// hand out BMP data while the caller asked for a PNG.
/// </summary>
public static class ImageSniffer
{
    public const string UnknownContentType = "application/octet-stream";

    public static string DetectContentType(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length >= 8 &&
            bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47)
        {
            return "image/png";
        }

        if (bytes.Length >= 3 && bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF)
        {
            return "image/jpeg";
        }

        if (bytes.Length >= 6 && StartsWith(bytes, "GIF8"u8))
        {
            return "image/gif";
        }

        if (bytes.Length >= 12 && StartsWith(bytes, "RIFF"u8) && StartsWith(bytes[8..], "WEBP"u8))
        {
            return "image/webp";
        }

        if (bytes.Length >= 2 && bytes[0] == 0x42 && bytes[1] == 0x4D)
        {
            return "image/bmp";
        }

        return UnknownContentType;
    }

    public static string ExtensionFor(string? contentType) => contentType switch
    {
        "image/png" => ".png",
        "image/jpeg" => ".jpg",
        "image/gif" => ".gif",
        "image/webp" => ".webp",
        "image/bmp" => ".bmp",
        _ => ".bin",
    };

    private static bool StartsWith(ReadOnlySpan<byte> bytes, ReadOnlySpan<byte> prefix) =>
        bytes.Length >= prefix.Length && bytes[..prefix.Length].SequenceEqual(prefix);
}
