#if WINDOWS
using NAudio.Wave;
#endif
using BinSoundTech.Models;

namespace BinSoundTech.Services;

/// <summary>
/// Service for enumerating and managing audio input devices.
/// </summary>
public class AudioDeviceService
{
    /// <summary>
    /// Gets a list of available audio input devices (microphones).
    /// </summary>
    public List<AudioDevice> GetInputDevices()
    {
        var devices = new List<AudioDevice>();

#if WINDOWS
        for (int i = 0; i < WaveInEvent.DeviceCount; i++)
        {
            var capabilities = WaveInEvent.GetCapabilities(i);
            devices.Add(new AudioDevice
            {
                DeviceIndex = i,
                Name = capabilities.ProductName,
                Channels = capabilities.Channels
            });
        }
#endif

        return devices;
    }

    /// <summary>
    /// Gets the default audio input device.
    /// </summary>
    public AudioDevice? GetDefaultInputDevice()
    {
        var devices = GetInputDevices();
        return devices.Count > 0 ? devices[0] : null;
    }
}
