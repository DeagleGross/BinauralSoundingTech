using BinSoundTech.Services;

namespace BinSoundTech.ViewModels;

/// <summary>
/// ViewModel for the main page containing binaural panning controls.
/// Azimuth and Elevation values match the CIPIC HRTF database ranges.
/// </summary>
public class MainPageViewModel : BaseViewModel
{
    // CIPIC database azimuth values: -80 to 80 degrees
    private const float MinAzimuth = -80f;
    private const float MaxAzimuth = 80f;

    // CIPIC database elevation values: -45 to 231 degrees  
    private const float MinElevation = -45f;
    private const float MaxElevation = 231f;

    private float _azimuth;
    private float _elevation;
    private bool _useCrossover;
    private int _crossoverFrequency = 150;
    private HrtfData? _currentHrtfData;
    private bool _isLoadingHrtf;
    private string _loadingStatus = "Ready";
    private string _currentAudioFilePath = string.Empty;
    private string _currentAudioFileName = "No file loaded";
    private readonly AudioPlaybackService _audioPlaybackService;
    private MicrophoneMonitoringService? _microphoneMonitoringService;
    private bool _isMicrophoneMonitoring;
    private float _microphoneLevel;

    /// <summary>
    /// Azimuth angle in degrees (-80 to 80).
    /// Positive values = right, Negative values = left.
    /// </summary>
    public float Azimuth
    {
        get => _azimuth;
        set
        {
            if (SetProperty(ref _azimuth, MathF.Round(value, 1)))
            {
                OnPropertyChanged(nameof(AzimuthDisplay));
                OnAzimuthChanged();
            }
        }
    }

    /// <summary>
    /// Elevation angle in degrees (-45 to 231).
    /// 0 = ear level, positive = up, >180 = behind/below.
    /// </summary>
    public float Elevation
    {
        get => _elevation;
        set
        {
            if (SetProperty(ref _elevation, MathF.Round(value, 1)))
            {
                OnPropertyChanged(nameof(ElevationDisplay));
                OnElevationChanged();
            }
        }
    }

    /// <summary>
    /// Whether to use a crossover filter for binaural processing.
    /// </summary>
    public bool UseCrossover
    {
        get => _useCrossover;
        set => SetProperty(ref _useCrossover, value);
    }

    /// <summary>
    /// Crossover frequency in Hz.
    /// </summary>
    public int CrossoverFrequency
    {
        get => _crossoverFrequency;
        set => SetProperty(ref _crossoverFrequency, value);
    }

    // Display properties for UI
    public string AzimuthDisplay => $"{Azimuth:F1}�";
    public string ElevationDisplay => $"{Elevation:F1}�";

    // Range properties for sliders
    public float AzimuthMinimum => MinAzimuth;
    public float AzimuthMaximum => MaxAzimuth;
    public float ElevationMinimum => MinElevation;
    public float ElevationMaximum => MaxElevation;

    /// <summary>
    /// Current loaded HRTF data, if any.
    /// </summary>
    public HrtfData? CurrentHrtfData
    {
        get => _currentHrtfData;
        set => SetProperty(ref _currentHrtfData, value);
    }

    /// <summary>
    /// Whether HRTF data is currently being loaded.
    /// </summary>
    public bool IsLoadingHrtf
    {
        get => _isLoadingHrtf;
        set => SetProperty(ref _isLoadingHrtf, value);
    }

    /// <summary>
    /// Status message for HRTF loading.
    /// </summary>
    public string LoadingStatus
    {
        get => _loadingStatus;
        set => SetProperty(ref _loadingStatus, value);
    }

    /// <summary>
    /// Currently loaded audio file path.
    /// </summary>
    public string CurrentAudioFilePath
    {
        get => _currentAudioFilePath;
        set => SetProperty(ref _currentAudioFilePath, value);
    }

    /// <summary>
    /// Display name of the currently loaded audio file.
    /// </summary>
    public string CurrentAudioFileName
    {
        get => _currentAudioFileName;
        set => SetProperty(ref _currentAudioFileName, value);
    }

    /// <summary>
    /// Gets the direction description based on current azimuth/elevation.
    /// </summary>
    public string DirectionDescription
    {
        get
        {
            var horizontal = Azimuth switch
            {
                < -30 => "Far Left",
                < -10 => "Left",
                < 10 => "Center",
                < 30 => "Right",
                _ => "Far Right"
            };

            var vertical = Elevation switch
            {
                < -20 => "Below",
                < 20 => "Level",
                < 90 => "Above",
                < 180 => "Behind-Above",
                _ => "Behind-Below"
            };

            return $"{horizontal}, {vertical}";
        }
    }

    /// <summary>
    /// Gets whether microphone monitoring is currently active.
    /// </summary>
    public bool IsMicrophoneMonitoring
    {
        get => _isMicrophoneMonitoring;
        set => SetProperty(ref _isMicrophoneMonitoring, value);
    }

    /// <summary>
    /// Gets the current microphone level (0.0 to 1.0).
    /// </summary>
    public float MicrophoneLevel
    {
        get => _microphoneLevel;
        set => SetProperty(ref _microphoneLevel, value);
    }

    public MainPageViewModel(AudioPlaybackService audioPlaybackService)
    {
        // Initialize to center position (directly in front at ear level)
        _azimuth = 0;
        _elevation = 0;

        // Initialize audio playback service (injected via DI)
        _audioPlaybackService = audioPlaybackService;
        _audioPlaybackService.PlaybackCompleted += OnPlaybackCompleted;
        
        // Bootstrap: Load HRTF data on initialization
        _ = InitializeHrtfDataAsync();
    }

    /// <summary>
    /// Initializes HRTF data from CIPIC_RIRS directory on app startup.
    /// Automatically loads the first available subject.
    /// </summary>
    private async Task InitializeHrtfDataAsync()
    {
        try
        {
            IsLoadingHrtf = true;
            LoadingStatus = "Loading HRTF data...";

            // Try multiple paths to locate CIPIC_RIRS
            var cipicPaths = new[]
            {
                // Try relative path from executable
                Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "CIPIC_RIRS"),
                // Try from project root
                Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "CIPIC_RIRS"),
                // Try absolute path (if running from bin/Debug)
                Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "CIPIC_RIRS"),
                // Try from current directory
                Path.Combine(Environment.CurrentDirectory, "CIPIC_RIRS"),
                // Try from desktop project root
                @"C:\Users\guram\Desktop\FP Project\BinauralSoundingTech\CIPIC_RIRS"
            };

            string? cipicDirectory = null;
            foreach (var path in cipicPaths)
            {
                var normalizedPath = Path.GetFullPath(path);
                if (Directory.Exists(normalizedPath))
                {
                    cipicDirectory = normalizedPath;
                    break;
                }
            }

            if (cipicDirectory == null || !Directory.Exists(cipicDirectory))
            {
                LoadingStatus = "CIPIC_RIRS directory not found";
#if DEBUG
                System.Diagnostics.Debug.WriteLine($"Could not find CIPIC_RIRS. Searched paths:\n{string.Join("\n", cipicPaths.Select(p => Path.GetFullPath(p)))}");
#endif
                return;
            }

            // Discover available subjects
            var subjects = CipicHrtfLoader.DiscoverSubjects(cipicDirectory);
            
            if (subjects.Count == 0)
            {
                LoadingStatus = "No CIPIC subjects found";
                return;
            }

            // Load the first available subject
            var firstSubject = subjects.First();
            LoadingStatus = $"Loading {Path.GetFileName(firstSubject)}...";
            
            CurrentHrtfData = await CipicHrtfLoader.LoadSubjectAsync(firstSubject);
            LoadingStatus = $"Loaded {Path.GetFileName(firstSubject)}";
        }
        catch (Exception ex)
        {
            LoadingStatus = $"Error: {ex.Message}";
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"HRTF Loading Error: {ex}");
#endif
        }
        finally
        {
            IsLoadingHrtf = false;
        }
    }

    private void OnAzimuthChanged()
    {
        OnPropertyChanged(nameof(DirectionDescription));
        // Update binaural effect in real-time for file playback
        _audioPlaybackService.Azimuth = Azimuth;
        // Update binaural effect for live microphone monitoring
        if (_microphoneMonitoringService != null && IsMicrophoneMonitoring)
        {
            _microphoneMonitoringService.Azimuth = Azimuth;
        }
    }

    private void OnElevationChanged()
    {
        OnPropertyChanged(nameof(DirectionDescription));
        // Update binaural effect in real-time for file playback
        _audioPlaybackService.Elevation = Elevation;
        // Update binaural effect for live microphone monitoring
        if (_microphoneMonitoringService != null && IsMicrophoneMonitoring)
        {
            _microphoneMonitoringService.Elevation = Elevation;
        }
    }

    /// <summary>
    /// Opens a file picker to select a WAV audio file.
    /// </summary>
    public async Task SelectAudioFileAsync()
    {
        try
        {
            var result = await FilePicker.Default.PickAsync(new PickOptions
            {
                FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
                {
                    { DevicePlatform.iOS, new[] { "public.audio" } },
                    { DevicePlatform.Android, new[] { "audio/*" } },
                    { DevicePlatform.WinUI, new[] { ".wav", ".mp3", ".m4a" } },
                    { DevicePlatform.macOS, new[] { "public.audio" } }
                }),
                PickerTitle = "Select an audio file"
            });

            if (result != null)
            {
                CurrentAudioFilePath = result.FullPath;
                CurrentAudioFileName = result.FileName;
            }
        }
        catch (Exception ex)
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"Error picking file: {ex}");
#endif
        }
    }

    /// <summary>
    /// Plays the currently loaded audio file.
    /// </summary>
    public async Task PlayAudioAsync()
    {
        if (string.IsNullOrEmpty(CurrentAudioFilePath))
        {
            LoadingStatus = "No audio file loaded";
            return;
        }

        try
        {
            LoadingStatus = "Playing...";
            
            // Load HRTF data if available
            if (CurrentHrtfData != null)
            {
                _audioPlaybackService.LoadHrtfData(CurrentHrtfData);
            }
            
            // Set azimuth and elevation
            _audioPlaybackService.Azimuth = Azimuth;
            _audioPlaybackService.Elevation = Elevation;
            
            // Load and play the audio file
            _audioPlaybackService.Load(CurrentAudioFilePath);
            _audioPlaybackService.Play();
            
            LoadingStatus = "Playing";
        }
        catch (Exception ex)
        {
            LoadingStatus = $"Error: {ex.Message}";
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"Playback Error: {ex}");
#endif
        }
    }

    /// <summary>
    /// Pauses audio playback.
    /// </summary>
    public void PauseAudio()
    {
        _audioPlaybackService?.Stop();
        LoadingStatus = "Paused";
    }

    /// <summary>
    /// Stops audio playback.
    /// </summary>
    public void StopAudio()
    {
        _audioPlaybackService?.Stop();
        LoadingStatus = "Stopped";
    }

    /// <summary>
    /// Toggles microphone monitoring on or off.
    /// </summary>
    public void ToggleMicrophoneMonitoring()
    {
        if (IsMicrophoneMonitoring)
        {
            StopMicrophoneMonitoring();
        }
        else
        {
            StartMicrophoneMonitoring();
        }
    }

    /// <summary>
    /// Starts microphone monitoring.
    /// </summary>
    public void StartMicrophoneMonitoring()
    {
        try
        {
#if WINDOWS
            _microphoneMonitoringService ??= new MicrophoneMonitoringService();
            
            // Load HRTF data if available
            if (CurrentHrtfData != null)
            {
                _microphoneMonitoringService.LoadHrtfData(CurrentHrtfData);
            }
            
            // Set current azimuth and elevation
            _microphoneMonitoringService.Azimuth = Azimuth;
            _microphoneMonitoringService.Elevation = Elevation;
            
            _microphoneMonitoringService.LevelUpdated += OnMicrophoneLevelUpdated;
            _microphoneMonitoringService.MonitoringStopped += OnMicrophoneMonitoringStopped;
            _microphoneMonitoringService.StartMonitoring();
            IsMicrophoneMonitoring = true;
            LoadingStatus = "Microphone monitoring active - move sliders to change sound direction";
#else
            LoadingStatus = "Microphone monitoring is only available on Windows";
#endif
        }
        catch (Exception ex)
        {
            LoadingStatus = $"Error: {ex.Message}";
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"Microphone monitoring error: {ex}");
#endif
        }
    }

    /// <summary>
    /// Stops microphone monitoring.
    /// </summary>
    public void StopMicrophoneMonitoring()
    {
        try
        {
            _microphoneMonitoringService?.StopMonitoring();
            IsMicrophoneMonitoring = false;
            MicrophoneLevel = 0f;
            LoadingStatus = "Microphone monitoring stopped";
        }
        catch (Exception ex)
        {
            LoadingStatus = $"Error: {ex.Message}";
        }
    }

    /// <summary>
    /// Handles microphone level updates.
    /// </summary>
    private void OnMicrophoneLevelUpdated(object? sender, AudioLevelEventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            MicrophoneLevel = e.Level;
        });
    }

    /// <summary>
    /// Handles when microphone monitoring stops.
    /// </summary>
    private void OnMicrophoneMonitoringStopped(object? sender, EventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            IsMicrophoneMonitoring = false;
            MicrophoneLevel = 0f;
        });
    }

    private void OnPlaybackCompleted(object? sender, EventArgs e)
    {
        LoadingStatus = "Playback completed";
    }
}
