using System.Globalization;

namespace SmtcReader.Cli;

/// <summary>
/// Hand rolled argument parsing. The surface is small enough that a dependency would
/// cost more than it saves, and this way the parsing rules are covered by tests.
/// </summary>
public static class CliParser
{
    public static CliParseResult Parse(IReadOnlyList<string> args)
    {
        ArgumentNullException.ThrowIfNull(args);

        var options = new CliOptions();
        var index = 0;

        while (index < args.Count)
        {
            var raw = args[index++];
            var separator = raw.IndexOf('=', StringComparison.Ordinal);
            var name = separator >= 0 ? raw[..separator] : raw;
            var inlineValue = separator >= 0 ? raw[(separator + 1)..] : null;

            string? TakeValue()
            {
                if (inlineValue is not null)
                {
                    return inlineValue;
                }

                if (index < args.Count && !LooksLikeOption(args[index]))
                {
                    return args[index++];
                }

                return null;
            }

            switch (name)
            {
                case "-h":
                case "--help":
                    return CliParseResult.ForHelp();

                case "--version":
                    return CliParseResult.ForVersion();

                case "-a":
                case "--app":
                    {
                        var value = TakeValue();
                        if (string.IsNullOrEmpty(value))
                        {
                            return CliParseResult.ForError($"Option '{name}' requires a value.");
                        }

                        options = options with { AppPattern = value };
                        break;
                    }

                case "-l":
                case "--list":
                    options = options with { ListOnly = true };
                    break;

                case "-w":
                case "--watch":
                    options = options with { Watch = true };
                    break;

                case "-i":
                case "--interval":
                    {
                        var value = TakeValue();
                        if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var interval) ||
                            interval <= 0)
                        {
                            return CliParseResult.ForError($"Option '{name}' requires a positive number of seconds.");
                        }

                        options = options with { Interval = interval };
                        break;
                    }

                case "-n":
                case "--count":
                    {
                        var value = TakeValue();
                        if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var count) ||
                            count < 0)
                        {
                            return CliParseResult.ForError($"Option '{name}' requires a non-negative integer.");
                        }

                        options = options with { Count = count };
                        break;
                    }

                case "-t":
                case "--thumbnail":
                    {
                        var value = TakeValue();
                        if (string.IsNullOrEmpty(value))
                        {
                            return CliParseResult.ForError($"Option '{name}' requires a path.");
                        }

                        options = options with { ThumbnailPath = value };
                        break;
                    }

                case "--out-dir":
                    {
                        var value = TakeValue();
                        if (string.IsNullOrEmpty(value))
                        {
                            return CliParseResult.ForError($"Option '{name}' requires a directory.");
                        }

                        options = options with { OutDir = value };
                        break;
                    }

                case "--no-markdown":
                    options = options with { NoMarkdown = true };
                    break;

                case "--raw":
                    options = options with { Raw = true };
                    break;

                case "-j":
                case "--json":
                    options = options with { Json = true };
                    break;

                case "--lang":
                    {
                        var value = TakeValue();
                        if (string.IsNullOrEmpty(value))
                        {
                            return CliParseResult.ForError($"Option '{name}' requires a value.");
                        }

                        options = options with { Lang = value };
                        break;
                    }

                default:
                    return CliParseResult.ForError($"Unknown option '{raw}'.");
            }
        }

        return CliParseResult.Success(options);
    }

    /// <summary>Reads <c>--lang</c> without validating the rest, so <c>--help</c> can be localized.</summary>
    public static string FindLanguage(IReadOnlyList<string> args)
    {
        ArgumentNullException.ThrowIfNull(args);

        for (var index = 0; index < args.Count; index++)
        {
            var arg = args[index];
            if (arg.StartsWith("--lang=", StringComparison.Ordinal))
            {
                return arg["--lang=".Length..];
            }

            if (arg == "--lang" && index + 1 < args.Count)
            {
                return args[index + 1];
            }
        }

        return "auto";
    }

    private static bool LooksLikeOption(string value) =>
        value.Length > 1 && value[0] == '-' && !char.IsDigit(value[1]);
}
