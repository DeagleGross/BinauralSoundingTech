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

    public MainPageViewModel()
    {
        // Initialize to center position (directly in front at ear level)
        _azimuth = 0;
        _elevation = 0;
        
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

            // Get the path to CIPIC_RIRS in the app bundle
            var cipicPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "CIPIC_RIRS");
            var normalizedPath = Path.GetFullPath(cipicPath);

            if (!Directory.Exists(normalizedPath))
            {
                LoadingStatus = "CIPIC_RIRS directory not found";
                return;
            }

            // Discover available subjects
            var subjects = CipicHrtfLoader.DiscoverSubjects(normalizedPath);
            
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
        // TODO: Update binaural effect when audio service is implemented
    }

    private void OnElevationChanged()
    {
        OnPropertyChanged(nameof(DirectionDescription));
        // TODO: Update binaural effect when audio service is implemented
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
            // TODO: Implement actual audio playback with binaural processing
            
            LoadingStatus = "Ready";
        }
        catch (Exception ex)
        {
            LoadingStatus = $"Error: {ex.Message}";
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"Playback Error: {ex}");
#endif
        }
    }
}
