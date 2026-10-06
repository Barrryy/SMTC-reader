using System.Globalization;

namespace SmtcReader.Formatting;

/// <summary>Value formatting shared by every renderer.</summary>
public static class Humanize
{
    public const string Dash = "—";

    public const string NullPlaceholder = "<null>";

    public const string EmptyPlaceholder = "<empty>";

    public static string Duration(TimeSpan? value)
    {
        if (value is null)
        {
            return Dash;
        }

        var span = value.Value;
        var hours = (int)Math.Floor(span.TotalHours);
        return string.Create(
            CultureInfo.InvariantCulture,
            $"{hours:00}:{span.Minutes:00}:{span.Seconds:00}.{span.Milliseconds:000}");
    }

    public static string Size(long? bytes)
    {
        if (bytes is null)
        {
            return Dash;
        }

        const double kilobyte = 1024;
        var value = bytes.Value;
        if (value >= kilobyte * kilobyte)
        {
            return string.Create(CultureInfo.InvariantCulture, $"{value / (kilobyte * kilobyte):N2} MB");
        }

        if (value >= kilobyte)
        {
            return string.Create(CultureInfo.InvariantCulture, $"{value / kilobyte:N2} KB");
        }

        return string.Create(CultureInfo.InvariantCulture, $"{value} B");
    }

    /// <summary>Renders a value for a Markdown table cell.</summary>
    public static string Cell(string? value)
    {
        if (string.IsNullOrEmpty(value) || value is NullPlaceholder or EmptyPlaceholder or "<none>")
        {
            return Dash;
        }

        return value.Replace("|", "\\|", StringComparison.Ordinal)
                    .Replace("\r", " ", StringComparison.Ordinal)
                    .Replace("\n", " ", StringComparison.Ordinal);
    }

    /// <summary>Renders a value for the console, keeping null/empty distinguishable.</summary>
    public static string Console(string? value) => value switch
    {
        null => NullPlaceholder,
        "" => EmptyPlaceholder,
        _ => value,
    };

    public static string Timestamp(DateTimeOffset value) =>
        value.Year < 2000
            ? Dash
            : value.ToString("yyyy-MM-dd HH:mm:ss.fff zzz", CultureInfo.InvariantCulture);

    public static string RawTimestamp(DateTimeOffset value) =>
        value.ToString("o", CultureInfo.InvariantCulture);
}
