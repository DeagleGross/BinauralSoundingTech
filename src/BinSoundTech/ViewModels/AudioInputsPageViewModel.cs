using System.Collections.ObjectModel;
using System.Windows.Input;
using BinSoundTech.Models;
using BinSoundTech.Services;

namespace BinSoundTech.ViewModels;

/// <summary>
/// ViewModel for the Audio Inputs configuration page.
/// Manages multiple audio input sources with individual binaural panning controls.
/// </summary>
public class AudioInputsPageViewModel : BaseViewModel
{
    private readonly AudioDeviceService _audioDeviceService;
    private readonly AudioPlaybackService _audioPlaybackService;
    private HrtfData? _currentHrtfData;
    private bool _isLoadingHrtf;
    private string _loadingStatus = "Ready";
    private AudioInputSource? _selectedSource;
    private AudioInputSource? _currentlyPlayingFileSource;

    // Dictionary to track microphone monitoring services per source
    private readonly Dictionary<string, MicrophoneMonitoringService> _microphoneServices = new();

    // CIPIC database ranges
    public const float MinAzimuth = -80f;
    public const float MaxAzimuth = 80f;
    public const float MinElevation = -45f;
    public const float MaxElevation = 231f;

    /// <summary>
    /// Collection of audio input sources.
    /// </summary>
    public ObservableCollection<AudioInputSource> AudioSources { get; } = new();

    /// <summary>
    /// Available audio input devices (microphones).
    /// </summary>
    public ObservableCollection<AudioDevice> AvailableDevices { get; } = new();

    /// <summary>
    /// Currently selected audio source.
    /// </summary>
    public AudioInputSource? SelectedSource
    {
        get => _selectedSource;
        set => SetProperty(ref _selectedSource, value);
    }

    /// <summary>
    /// Current loaded HRTF data.
    /// </summary>
    public HrtfData? CurrentHrtfData
    {
        get => _currentHrtfData;
        set => SetProperty(ref _currentHrtfData, value);
    }

    /// <summary>
    /// Whether HRTF data is being loaded.
    /// </summary>
    public bool IsLoadingHrtf
    {
        get => _isLoadingHrtf;
        set => SetProperty(ref _isLoadingHrtf, value);
    }

    /// <summary>
    /// Loading status message.
    /// </summary>
    public string LoadingStatus
    {
        get => _loadingStatus;
        set => SetProperty(ref _loadingStatus, value);
    }

    // Commands
    public ICommand AddFileSourceCommand { get; }
    public ICommand AddMicrophoneSourceCommand { get; }
    public ICommand RemoveSourceCommand { get; }
    public ICommand RefreshDevicesCommand { get; }

    public AudioInputsPageViewModel(AudioDeviceService audioDeviceService, AudioPlaybackService audioPlaybackService)
    {
        _audioDeviceService = audioDeviceService;
        _audioPlaybackService = audioPlaybackService;

        // Subscribe to playback completed event
        _audioPlaybackService.PlaybackCompleted += OnPlaybackCompleted;

        AddFileSourceCommand = new Command(AddFileSource);
        AddMicrophoneSourceCommand = new Command(AddMicrophoneSource);
        RemoveSourceCommand = new Command<AudioInputSource>(RemoveSource);
        RefreshDevicesCommand = new Command(RefreshDevices);

        // Initialize
        RefreshDevices();
        _ = InitializeHrtfDataAsync();
    }

    /// <summary>
    /// Handles playback completed event from AudioPlaybackService.
    /// </summary>
    private void OnPlaybackCompleted(object? sender, EventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (_currentlyPlayingFileSource != null)
            {
                _currentlyPlayingFileSource.IsActive = false;
                _currentlyPlayingFileSource.Level = 0;
                _currentlyPlayingFileSource = null;
                LoadingStatus = "Playback finished";
            }
        });
    }

    /// <summary>
    /// Refreshes the list of available audio devices.
    /// </summary>
    public void RefreshDevices()
    {
        AvailableDevices.Clear();
        var devices = _audioDeviceService.GetInputDevices();
        foreach (var device in devices)
        {
            AvailableDevices.Add(device);
        }
        LoadingStatus = $"Found {devices.Count} input device(s)";
    }

    /// <summary>
    /// Adds a new file-based audio source.
    /// </summary>
    public void AddFileSource()
    {
        var source = new AudioInputSource
        {
            Name = $"File {AudioSources.Count + 1}",
            InputType = AudioInputType.File,
            Azimuth = 0,
            Elevation = 0
        };
        SubscribeToSourceChanges(source);
        AudioSources.Add(source);
        SelectedSource = source;
    }

    /// <summary>
    /// Adds a new microphone-based audio source.
    /// </summary>
    public void AddMicrophoneSource()
    {
        var defaultDevice = AvailableDevices.FirstOrDefault();
        var source = new AudioInputSource
        {
            Name = $"Mic {AudioSources.Count + 1}",
            InputType = AudioInputType.Microphone,
            DeviceIndex = defaultDevice?.DeviceIndex ?? 0,
            DeviceName = defaultDevice?.Name ?? "Default Device",
            Azimuth = 0,
            Elevation = 0
        };
        SubscribeToSourceChanges(source);
        AudioSources.Add(source);
        SelectedSource = source;
    }

    /// <summary>
    /// Subscribes to property changes on a source to sync with services.
    /// </summary>
    private void SubscribeToSourceChanges(AudioInputSource source)
    {
        source.PropertyChanged += (s, e) =>
        {
            if (!source.IsActive) return;

            if (source.InputType == AudioInputType.Microphone)
            {
                if (_microphoneServices.TryGetValue(source.Id, out var micService))
                {
                    switch (e.PropertyName)
                    {
                        case nameof(AudioInputSource.Azimuth):
                            micService.Azimuth = source.Azimuth;
                            break;
                        case nameof(AudioInputSource.Elevation):
                            micService.Elevation = source.Elevation;
                            break;
                        case nameof(AudioInputSource.Volume):
                            micService.Volume = source.Volume;
                            break;
                        case nameof(AudioInputSource.IsMuted):
                            micService.IsMuted = source.IsMuted;
                            break;
                    }
                }
            }
            else if (source.InputType == AudioInputType.File)
            {
                switch (e.PropertyName)
                {
                    case nameof(AudioInputSource.Azimuth):
                        _audioPlaybackService.Azimuth = source.Azimuth;
                        break;
                    case nameof(AudioInputSource.Elevation):
                        _audioPlaybackService.Elevation = source.Elevation;
                        break;
                    case nameof(AudioInputSource.Volume):
                        _audioPlaybackService.Volume = source.Volume;
                        break;
                    case nameof(AudioInputSource.IsMuted):
                        _audioPlaybackService.IsMuted = source.IsMuted;
                        break;
                }
            }
        };
    }

    /// <summary>
    /// Removes an audio source.
    /// </summary>
    public void RemoveSource(AudioInputSource? source)
    {
        if (source != null)
        {
            // Stop if active
            if (source.IsActive)
            {
                StopSource(source);
            }
            
            // Clear currently playing reference if this was it
            if (_currentlyPlayingFileSource == source)
            {
                _currentlyPlayingFileSource = null;
            }
            
            // Dispose microphone service if exists
            if (_microphoneServices.TryGetValue(source.Id, out var micService))
            {
                micService.Dispose();
                _microphoneServices.Remove(source.Id);
            }
            
            AudioSources.Remove(source);
        }
    }

    /// <summary>
    /// Selects a file for the given audio source.
    /// </summary>
    public async Task SelectFileForSourceAsync(AudioInputSource source)
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
                source.FilePath = result.FullPath;
                source.FileName = result.FileName;
                source.Name = Path.GetFileNameWithoutExtension(result.FileName);
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
    /// Sets the device for a microphone source.
    /// </summary>
    public void SetDeviceForSource(AudioInputSource source, AudioDevice device)
    {
        if (source.InputType == AudioInputType.Microphone)
        {
            // Stop if currently monitoring
            if (source.IsActive)
            {
                StopSource(source);
            }
            
            source.DeviceIndex = device.DeviceIndex;
            source.DeviceName = device.Name;
        }
    }

    /// <summary>
    /// Toggles playback/monitoring for a source.
    /// </summary>
    public async Task ToggleSourceAsync(AudioInputSource source)
    {
        if (source.IsActive)
        {
            StopSource(source);
        }
        else
        {
            await StartSourceAsync(source);
        }
    }

    /// <summary>
    /// Toggles mute for a source.
    /// </summary>
    public void ToggleMute(AudioInputSource source)
    {
        source.IsMuted = !source.IsMuted;
    }

    /// <summary>
    /// Starts playback/monitoring for a source.
    /// </summary>
    public async Task StartSourceAsync(AudioInputSource source)
    {
        try
        {
            if (source.InputType == AudioInputType.File)
            {
                if (string.IsNullOrEmpty(source.FilePath))
                {
                    LoadingStatus = "No file selected";
                    return;
                }

                // Stop any currently playing file source
                if (_currentlyPlayingFileSource != null && _currentlyPlayingFileSource != source)
                {
                    _currentlyPlayingFileSource.IsActive = false;
                    _currentlyPlayingFileSource.Level = 0;
                }

                // Load HRTF data if available
                if (CurrentHrtfData != null)
                {
                    _audioPlaybackService.LoadHrtfData(CurrentHrtfData);
                }

                _audioPlaybackService.Azimuth = source.Azimuth;
                _audioPlaybackService.Elevation = source.Elevation;
                _audioPlaybackService.Volume = source.Volume;
                _audioPlaybackService.IsMuted = source.IsMuted;
                _audioPlaybackService.Load(source.FilePath);
                _audioPlaybackService.Play();
                
                source.IsActive = true;
                _currentlyPlayingFileSource = source;
                LoadingStatus = $"Playing: {source.Name}";
            }
            else if (source.InputType == AudioInputType.Microphone)
            {
                await StartMicrophoneMonitoringAsync(source);
            }
        }
        catch (Exception ex)
        {
            LoadingStatus = $"Error: {ex.Message}";
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"Error starting source: {ex}");
#endif
        }
    }

    /// <summary>
    /// Starts microphone monitoring for a source.
    /// </summary>
    private async Task StartMicrophoneMonitoringAsync(AudioInputSource source)
    {
#if WINDOWS
        try
        {
            // Create or get existing service for this source
            if (!_microphoneServices.TryGetValue(source.Id, out var micService))
            {
                micService = new MicrophoneMonitoringService();
                _microphoneServices[source.Id] = micService;
            }

            // Load HRTF data if available
            if (CurrentHrtfData != null)
            {
                micService.LoadHrtfData(CurrentHrtfData);
            }

            // Set binaural parameters and volume
            micService.Azimuth = source.Azimuth;
            micService.Elevation = source.Elevation;
            micService.Volume = source.Volume;
            micService.IsMuted = source.IsMuted;

            // Subscribe to level updates
            micService.LevelUpdated += (s, e) =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    source.Level = e.Level;
                });
            };

            micService.MonitoringStopped += (s, e) =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    source.IsActive = false;
                    source.Level = 0;
                });
            };

            // Start monitoring with specific device
            micService.StartMonitoring(source.DeviceIndex);
            
            source.IsActive = true;
            LoadingStatus = $"Monitoring: {source.DeviceName}";
        }
        catch (Exception ex)
        {
            LoadingStatus = $"Mic Error: {ex.Message}";
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"Error starting microphone: {ex}");
#endif
        }
#else
        LoadingStatus = "Microphone monitoring only available on Windows";
        await Task.CompletedTask;
#endif
    }

    /// <summary>
    /// Stops playback/monitoring for a source.
    /// </summary>
    public void StopSource(AudioInputSource source)
    {
        try
        {
            if (source.InputType == AudioInputType.File)
            {
                _audioPlaybackService.Stop();
                if (_currentlyPlayingFileSource == source)
                {
                    _currentlyPlayingFileSource = null;
                }
            }
            else if (source.InputType == AudioInputType.Microphone)
            {
                if (_microphoneServices.TryGetValue(source.Id, out var micService))
                {
                    micService.StopMonitoring();
                }
            }
            
            source.IsActive = false;
            source.Level = 0;
            LoadingStatus = "Stopped";
        }
        catch (Exception ex)
        {
            LoadingStatus = $"Error: {ex.Message}";
        }
    }

    /// <summary>
    /// Initializes HRTF data from CIPIC_RIRS directory.
    /// </summary>
    private async Task InitializeHrtfDataAsync()
    {
        try
        {
            IsLoadingHrtf = true;
            LoadingStatus = "Loading HRTF data...";

            var cipicDirectory = CipicPathResolver.FindCipicDirectory();

            if (cipicDirectory == null)
            {
                LoadingStatus = "CIPIC_RIRS directory not found";
                return;
            }

            var subjects = CipicHrtfLoader.DiscoverSubjects(cipicDirectory);

            if (subjects.Count == 0)
            {
                LoadingStatus = "No CIPIC subjects found";
                return;
            }

            var firstSubject = subjects.First();
            LoadingStatus = $"Loading {Path.GetFileName(firstSubject)}...";

            CurrentHrtfData = await CipicHrtfLoader.LoadSubjectAsync(firstSubject);
            LoadingStatus = $"Loaded {Path.GetFileName(firstSubject)}";
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
