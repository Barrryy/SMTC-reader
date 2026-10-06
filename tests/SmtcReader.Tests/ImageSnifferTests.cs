using SmtcReader.Smtc;
using Xunit;

namespace SmtcReader.Tests;

public class ImageSnifferTests
{
    [Fact]
    public void Detects_png()
    {
        Assert.Equal("image/png", ImageSniffer.DetectContentType(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }));
    }

    [Fact]
    public void Detects_jpeg()
    {
        Assert.Equal("image/jpeg", ImageSniffer.DetectContentType(new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 }));
    }

    [Fact]
    public void Detects_bmp_which_is_what_some_players_send()
    {
        Assert.Equal("image/bmp", ImageSniffer.DetectContentType(new byte[] { 0x42, 0x4D, 0x36, 0x00 }));
    }

    [Fact]
    public void Detects_webp()
    {
        var bytes = new byte[16];
        "RIFF"u8.CopyTo(bytes);
        "WEBP"u8.CopyTo(bytes.AsSpan(8));
        Assert.Equal("image/webp", ImageSniffer.DetectContentType(bytes));
    }

    [Fact]
    public void Falls_back_for_unknown_data()
    {
        Assert.Equal(ImageSniffer.UnknownContentType, ImageSniffer.DetectContentType(new byte[] { 0x00, 0x01, 0x02 }));
        Assert.Equal(ImageSniffer.UnknownContentType, ImageSniffer.DetectContentType(ReadOnlySpan<byte>.Empty));
    }

    [Fact]
    public void Extension_matches_the_detected_type()
    {
        Assert.Equal(".png", ImageSniffer.ExtensionFor("image/png"));
        Assert.Equal(".bmp", ImageSniffer.ExtensionFor("image/bmp"));
        Assert.Equal(".bin", ImageSniffer.ExtensionFor(ImageSniffer.UnknownContentType));
        Assert.Equal(".bin", ImageSniffer.ExtensionFor(null));
    }
}
