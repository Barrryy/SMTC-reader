using System.Globalization;

namespace SmtcReader.Rendering;

/// <summary>
/// Padding that accounts for East Asian wide characters, so columns line up in both
/// English and Chinese output.
/// </summary>
public static class TextLayout
{
    public static int DisplayWidth(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return 0;
        }

        var width = 0;
        foreach (var rune in value.EnumerateRunes())
        {
            width += IsWide(rune.Value) ? 2 : 1;
        }

        return width;
    }

    public static string Pad(string? value, int width)
    {
        value ??= string.Empty;
        var padding = width - DisplayWidth(value);
        return padding > 0 ? value + new string(' ', padding) : value;
    }

    public static string PadLeft(string? value, int width)
    {
        value ??= string.Empty;
        var padding = width - DisplayWidth(value);
        return padding > 0 ? new string(' ', padding) + value : value;
    }

    public static string Truncate(string? value, int maxWidth)
    {
        if (string.IsNullOrEmpty(value) || DisplayWidth(value) <= maxWidth)
        {
            return value ?? string.Empty;
        }

        var builder = new System.Text.StringBuilder();
        var used = 0;
        foreach (var rune in value.EnumerateRunes())
        {
            var runeWidth = IsWide(rune.Value) ? 2 : 1;
            if (used + runeWidth > maxWidth - 1)
            {
                break;
            }

            builder.Append(rune.ToString());
            used += runeWidth;
        }

        return builder.Append('…').ToString();
    }

    private static bool IsWide(int codePoint) => codePoint switch
    {
        >= 0x1100 and <= 0x115F => true,   // Hangul Jamo
        >= 0x2E80 and <= 0x303E => true,   // CJK radicals, Kangxi, CJK punctuation
        >= 0x3041 and <= 0x33FF => true,   // Kana, Hangul compatibility, CJK symbols
        >= 0x3400 and <= 0x4DBF => true,   // CJK extension A
        >= 0x4E00 and <= 0x9FFF => true,   // CJK unified ideographs
        >= 0xA000 and <= 0xA4CF => true,   // Yi
        >= 0xAC00 and <= 0xD7A3 => true,   // Hangul syllables
        >= 0xF900 and <= 0xFAFF => true,   // CJK compatibility ideographs
        >= 0xFE30 and <= 0xFE6F => true,   // CJK compatibility forms
        >= 0xFF00 and <= 0xFF60 => true,   // Fullwidth forms
        >= 0xFFE0 and <= 0xFFE6 => true,   // Fullwidth signs
        >= 0x20000 and <= 0x3FFFD => true, // CJK extensions B+
        _ => false,
    };

    public static string Invariant(FormattableString value) =>
        value.ToString(CultureInfo.InvariantCulture);
}
