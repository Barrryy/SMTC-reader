using SmtcReader.Smtc;
using Windows.Media.Control;
using Xunit;

namespace SmtcReader.Tests;

/// <summary>
/// Guards against API drift. These tests read the real Windows metadata, so they need
/// no media session but they do need to run on Windows.
/// </summary>
public class ControlCapabilitiesTests
{
    [Fact]
    public void Every_known_capability_still_exists_in_windows()
    {
        var type = typeof(GlobalSystemMediaTransportControlsSessionPlaybackControls);

        foreach (var capability in ControlCapabilities.All)
        {
            Assert.NotNull(type.GetProperty(capability.Property));
        }
    }

    [Fact]
    public void The_known_list_covers_the_whole_windows_surface()
    {
        var type = typeof(GlobalSystemMediaTransportControlsSessionPlaybackControls);
        var actual = type.GetProperties()
            .Select(property => property.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();
        var known = ControlCapabilities.All
            .Select(capability => capability.Property)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        // When this fails, a Windows update added or removed a capability flag:
        // add it to ControlCapabilities.All (and its label) to keep the report complete.
        Assert.Equal(actual, known);
    }

    [Fact]
    public void Labels_fall_back_to_the_property_name()
    {
        Assert.Equal("Play", ControlCapabilities.Label("IsPlayEnabled", zh: false));
        Assert.Equal("播放", ControlCapabilities.Label("IsPlayEnabled", zh: true));
        Assert.Equal("IsSomethingNew", ControlCapabilities.Label("IsSomethingNew", zh: true));
    }
}
