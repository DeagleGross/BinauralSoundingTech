using BinSoundTech.Services;

namespace BinSoundTech.ViewModels;

/// <summary>
/// ViewModel for the main page showing app description and navigation.
/// </summary>
public class MainPageViewModel : BaseViewModel
{
    private bool _isLoadingHrtf;
    private string _loadingStatus = "Ready";

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

    public MainPageViewModel()
    {
        // Check HRTF data availability on startup
        _ = CheckHrtfDataAsync();
    }

    /// <summary>
    /// Checks if HRTF data is available.
    /// </summary>
    private async Task CheckHrtfDataAsync()
    {
        try
        {
            IsLoadingHrtf = true;
            LoadingStatus = "Checking HRTF data...";

            var cipicDirectory = CipicPathResolver.FindCipicDirectory();

            if (cipicDirectory == null)
            {
                LoadingStatus = "HRTF data not found - place CIPIC_RIRS folder in repository root";
                return;
            }

            var subjects = CipicHrtfLoader.DiscoverSubjects(cipicDirectory);

            if (subjects.Count == 0)
            {
                LoadingStatus = "No CIPIC subjects found";
                return;
            }

            LoadingStatus = $"HRTF ready - {subjects.Count} subject(s) available";
        }
        catch (Exception ex)
        {
            LoadingStatus = $"Error: {ex.Message}";
        }
        finally
        {
            IsLoadingHrtf = false;
        }
    }
}
