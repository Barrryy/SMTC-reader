using SmtcReader.Cli;
using Xunit;

namespace SmtcReader.Tests;

public class CliParserTests
{
    [Fact]
    public void Parses_long_options()
    {
        var result = CliParser.Parse(new[]
        {
            "--app", "spotify", "--watch", "--interval", "0.5", "--count", "3",
            "--thumbnail", "cover.png", "--out-dir", "reports", "--raw", "--json", "--lang", "zh",
        });

        Assert.Null(result.Error);
        var options = Assert.IsType<CliOptions>(result.Options);
        Assert.Equal("spotify", options.AppPattern);
        Assert.True(options.Watch);
        Assert.Equal(0.5, options.Interval);
        Assert.Equal(3, options.Count);
        Assert.Equal("cover.png", options.ThumbnailPath);
        Assert.Equal("reports", options.OutDir);
        Assert.True(options.Raw);
        Assert.True(options.Json);
        Assert.Equal("zh", options.Lang);
    }

    [Fact]
    public void Supports_inline_values_and_short_flags()
    {
        var result = CliParser.Parse(new[] { "--app=pot", "-l", "-i0.25" });

        // -i0.25 is not a supported form; it must be rejected rather than silently ignored.
        Assert.NotNull(result.Error);

        var ok = CliParser.Parse(new[] { "--app=pot", "-l", "-i", "0.25" });
        Assert.Null(ok.Error);
        Assert.Equal("pot", ok.Options!.AppPattern);
        Assert.True(ok.Options.ListOnly);
        Assert.Equal(0.25, ok.Options.Interval);
    }

    [Fact]
    public void Reports_missing_values()
    {
        Assert.NotNull(CliParser.Parse(new[] { "--app" }).Error);
        Assert.NotNull(CliParser.Parse(new[] { "--thumbnail" }).Error);
        Assert.NotNull(CliParser.Parse(new[] { "--interval", "abc" }).Error);
        Assert.NotNull(CliParser.Parse(new[] { "--count", "-1" }).Error);
    }

    [Fact]
    public void Reports_unknown_options()
    {
        var result = CliParser.Parse(new[] { "--nope" });

        Assert.Contains("--nope", result.Error, StringComparison.Ordinal);
    }

    [Fact]
    public void Help_and_version_short_circuit()
    {
        Assert.True(CliParser.Parse(new[] { "--help" }).Help);
        Assert.True(CliParser.Parse(new[] { "--version" }).Version);
        Assert.True(CliParser.Parse(new[] { "--nope", "--help" }).Error is not null);
    }

    [Fact]
    public void FindLanguage_reads_the_flag_without_parsing_everything_else()
    {
        Assert.Equal("zh", CliParser.FindLanguage(new[] { "--help", "--lang", "zh" }));
        Assert.Equal("en", CliParser.FindLanguage(new[] { "--lang=en" }));
        Assert.Equal("auto", CliParser.FindLanguage(new[] { "--help" }));
    }
}
