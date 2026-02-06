namespace BinSoundTech.Models;

/// <summary>
/// Represents the type of audio input source.
/// </summary>
public enum AudioInputType
{
    /// <summary>
    /// Audio from a file (WAV, MP3, etc.)
    /// </summary>
    File,
    
    /// <summary>
    /// Audio from a microphone or audio input device
    /// </summary>
    Microphone
}

/// <summary>
/// Represents an audio input source with binaural panning parameters.
/// Each row in the input configuration represents one audio source.
/// </summary>
public class AudioInputSource : ViewModels.BaseViewModel
{
    private string _id = Guid.NewGuid().ToString();
    private string _name = "New Input";
    private AudioInputType _inputType = AudioInputType.File;
    private string? _filePath;
    private string? _fileName;
    private int _deviceIndex = -1;
    private string? _deviceName;
    private float _azimuth;
    private float _elevation;
    private bool _isActive;
    private bool _isMuted;
    private float _level;
    private float _volume = 1.0f;

    /// <summary>
    /// Unique identifier for this audio source.
    /// </summary>
    public string Id
    {
        get => _id;
        set => SetProperty(ref _id, value);
    }

    /// <summary>
    /// Display name for this audio source.
    /// </summary>
    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    /// <summary>
    /// Type of audio input (File or Microphone).
    /// </summary>
    public AudioInputType InputType
    {
        get => _inputType;
        set
        {
            if (SetProperty(ref _inputType, value))
            {
                OnPropertyChanged(nameof(IsFileInput));
                OnPropertyChanged(nameof(IsMicrophoneInput));
                OnPropertyChanged(nameof(SourceDescription));
            }
        }
    }

    /// <summary>
    /// Path to the audio file (when InputType is File).
    /// </summary>
    public string? FilePath
    {
        get => _filePath;
        set
        {
            if (SetProperty(ref _filePath, value))
            {
                OnPropertyChanged(nameof(SourceDescription));
            }
        }
    }

    /// <summary>
    /// Display name of the audio file.
    /// </summary>
    public string? FileName
    {
        get => _fileName;
        set
        {
            if (SetProperty(ref _fileName, value))
            {
                OnPropertyChanged(nameof(SourceDescription));
            }
        }
    }

    /// <summary>
    /// Index of the audio input device (when InputType is Microphone).
    /// </summary>
    public int DeviceIndex
    {
        get => _deviceIndex;
        set => SetProperty(ref _deviceIndex, value);
    }

    /// <summary>
    /// Name of the audio input device.
    /// </summary>
    public string? DeviceName
    {
        get => _deviceName;
        set
        {
            if (SetProperty(ref _deviceName, value))
            {
                OnPropertyChanged(nameof(SourceDescription));
            }
        }
    }

    /// <summary>
    /// Azimuth angle in degrees (-80 to 80).
    /// </summary>
    public float Azimuth
    {
        get => _azimuth;
        set
        {
            if (SetProperty(ref _azimuth, MathF.Round(value, 1)))
            {
                OnPropertyChanged(nameof(AzimuthDisplay));
                OnPropertyChanged(nameof(DirectionDescription));
            }
        }
    }

    /// <summary>
    /// Elevation angle in degrees (-45 to 231).
    /// </summary>
    public float Elevation
    {
        get => _elevation;
        set
        {
            if (SetProperty(ref _elevation, MathF.Round(value, 1)))
            {
                OnPropertyChanged(nameof(ElevationDisplay));
                OnPropertyChanged(nameof(DirectionDescription));
            }
        }
    }

    /// <summary>
    /// Whether this audio source is currently active (playing/monitoring).
    /// </summary>
    public bool IsActive
    {
        get => _isActive;
        set => SetProperty(ref _isActive, value);
    }

    /// <summary>
    /// Whether this audio source is muted.
    /// </summary>
    public bool IsMuted
    {
        get => _isMuted;
        set
        {
            if (SetProperty(ref _isMuted, value))
            {
                OnPropertyChanged(nameof(MuteButtonText));
            }
        }
    }

    /// <summary>
    /// Current audio level (0.0 to 1.0).
    /// </summary>
    public float Level
    {
        get => _level;
        set => SetProperty(ref _level, value);
    }

    /// <summary>
    /// Volume level (0.0 to 1.0).
    /// </summary>
    public float Volume
    {
        get => _volume;
        set
        {
            if (SetProperty(ref _volume, Math.Clamp(value, 0f, 1f)))
            {
                OnPropertyChanged(nameof(VolumeDisplay));
            }
        }
    }

    // Computed properties
    public bool IsFileInput => InputType == AudioInputType.File;
    public bool IsMicrophoneInput => InputType == AudioInputType.Microphone;
    public string AzimuthDisplay => $"{Azimuth:F0}°";
    public string ElevationDisplay => $"{Elevation:F0}°";
    public string VolumeDisplay => $"{Volume * 100:F0}%";
    public string MuteButtonText => IsMuted ? "M" : "Vol";

    /// <summary>
    /// Gets a description of the audio source.
    /// </summary>
    public string SourceDescription
    {
        get
        {
            if (InputType == AudioInputType.File)
            {
                return string.IsNullOrEmpty(FileName) ? "No file selected" : FileName;
            }
            else
            {
                return string.IsNullOrEmpty(DeviceName) ? "No device selected" : DeviceName;
            }
        }
    }

    /// <summary>
    /// Gets a human-readable description of the sound direction based on azimuth and elevation.
    /// </summary>
    public string DirectionDescription
    {
        get
        {
            // Horizontal direction based on azimuth
            var horizontal = Azimuth switch
            {
                < -60 => "Far Left",
                < -30 => "Left",
                < -10 => "Slight Left",
                < 10 => "Center",
                < 30 => "Slight Right",
                < 60 => "Right",
                _ => "Far Right"
            };

            // Vertical direction based on elevation
            // CIPIC elevation: -45 (below) to 90 (above) to 180 (behind) to 231 (behind-below)
            var vertical = Elevation switch
            {
                < -20 => "Below",
                < 20 => "Level",
                < 70 => "Above",
                < 110 => "High Above",
                < 160 => "Behind-Above",
                < 200 => "Behind",
                _ => "Behind-Below"
            };

            return $"{horizontal}, {vertical}";
        }
    }
}
