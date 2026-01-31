namespace BinSoundTech.Models;

/// <summary>
/// Represents an audio input device (microphone).
/// </summary>
public class AudioDevice
{
    /// <summary>
    /// Device index for NAudio.
    /// </summary>
    public int DeviceIndex { get; set; }

    /// <summary>
    /// Display name of the device.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Number of input channels.
    /// </summary>
    public int Channels { get; set; }

    public override string ToString() => Name;
}
