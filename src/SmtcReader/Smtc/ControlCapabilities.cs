using Windows.Media.Control;

namespace SmtcReader.Smtc;

/// <summary>Maps the capability set to a stable, ordered, human readable list.</summary>
public sealed record ControlCapability(string Property, string En, string Zh);

public static class ControlCapabilities
{
    /// <summary>
    /// Order matters: this is the order used by every renderer. The set is read by
    /// reflection, so a new Windows build adding a flag will not break us, and
    /// <c>ControlCapabilitiesTests</c> fails when the API grows and this list is stale.
    /// </summary>
    public static readonly IReadOnlyList<ControlCapability> All = new ControlCapability[]
    {
        new("IsPlayEnabled", "Play", "播放"),
        new("IsPauseEnabled", "Pause", "暂停"),
        new("IsPlayPauseToggleEnabled", "Play/pause toggle", "播放/暂停切换"),
        new("IsStopEnabled", "Stop", "停止"),
        new("IsNextEnabled", "Next", "下一个"),
        new("IsPreviousEnabled", "Previous", "上一个"),
        new("IsFastForwardEnabled", "Fast forward", "快进"),
        new("IsRewindEnabled", "Rewind", "快退"),
        new("IsShuffleEnabled", "Shuffle", "随机播放"),
        new("IsRepeatEnabled", "Repeat", "循环"),
        new("IsPlaybackRateEnabled", "Playback rate", "倍速"),
        new("IsPlaybackPositionEnabled", "Seek", "拖动进度"),
        new("IsChannelUpEnabled", "Channel up", "频道 +"),
        new("IsChannelDownEnabled", "Channel down", "频道 -"),
        new("IsRecordEnabled", "Record", "录制"),
    };

    public static string Label(string property, bool zh)
    {
        foreach (var capability in All)
        {
            if (string.Equals(capability.Property, property, StringComparison.Ordinal))
            {
                return zh ? capability.Zh : capability.En;
            }
        }

        return property;
    }

    public static IReadOnlyList<ControlState> Read(GlobalSystemMediaTransportControlsSessionPlaybackControls? controls)
    {
        if (controls is null)
        {
            return Array.Empty<ControlState>();
        }

        var states = new List<ControlState>(All.Count);
        var type = controls.GetType();
        foreach (var capability in All)
        {
            var enabled = type.GetProperty(capability.Property)?.GetValue(controls) is true;
            states.Add(new ControlState(capability.Property, enabled));
        }

        return states;
    }
}
