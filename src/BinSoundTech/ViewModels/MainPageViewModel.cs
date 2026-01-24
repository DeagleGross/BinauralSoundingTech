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
    public string AzimuthDisplay => $"{Azimuth:F1}°";
    public string ElevationDisplay => $"{Elevation:F1}°";

    // Range properties for sliders
    public float AzimuthMinimum => MinAzimuth;
    public float AzimuthMaximum => MaxAzimuth;
    public float ElevationMinimum => MinElevation;
    public float ElevationMaximum => MaxElevation;

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
}
