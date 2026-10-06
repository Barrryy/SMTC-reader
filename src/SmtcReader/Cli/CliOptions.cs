namespace SmtcReader.Cli;

/// <summary>Parsed command line.</summary>
public sealed record CliOptions
{
    public string? AppPattern { get; init; }

    public bool ListOnly { get; init; }

    public bool Watch { get; init; }

    public double Interval { get; init; } = 1.0;

    public int Count { get; init; }

    public string? ThumbnailPath { get; init; }

    public string? OutDir { get; init; }

    public bool NoMarkdown { get; init; }

    public bool Raw { get; init; }

    public bool Json { get; init; }

    public string Lang { get; init; } = "auto";
}

/// <summary>Outcome of parsing: either options, a request for help/version, or an error.</summary>
public sealed record CliParseResult(CliOptions? Options, bool Help, bool Version, string? Error)
{
    public static CliParseResult ForHelp() => new(null, true, false, null);

    public static CliParseResult ForVersion() => new(null, false, true, null);

    public static CliParseResult ForError(string message) => new(null, false, false, message);

    public static CliParseResult Success(CliOptions options) => new(options, false, false, null);
}
