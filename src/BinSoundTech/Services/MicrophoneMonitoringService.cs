#if WINDOWS
using NAudio.Wave;
#endif

namespace BinSoundTech.Services;

/// <summary>
/// Microphone monitoring service that captures and monitors live microphone input.
/// Uses NAudio for Windows audio capture with real-time level monitoring.
/// </summary>
public class MicrophoneMonitoringService : IDisposable
{
#if WINDOWS
    private IWaveIn? _waveIn;
    private WaveInBuffer[] _buffers = Array.Empty<WaveInBuffer>();
    private volatile bool _isMonitoring;
    private float[] _levelBuffer = new float[2048];
    private int _bufferPosition;
    private readonly object _levelLock = new();
#endif

    /// <summary>
    /// Event raised when audio level is updated.
    /// </summary>
    public event EventHandler<AudioLevelEventArgs>? LevelUpdated;

    /// <summary>
    /// Event raised when monitoring stops.
    /// </summary>
    public event EventHandler? MonitoringStopped;

    /// <summary>
    /// Gets whether microphone monitoring is currently active.
    /// </summary>
    public bool IsMonitoring
    {
#if WINDOWS
        get => _isMonitoring;
#else
        get => false;
#endif
    }

    /// <summary>
    /// Gets the current microphone level (0.0 to 1.0).
    /// </summary>
    public float CurrentLevel
    {
        get
        {
#if WINDOWS
            lock (_levelLock)
            {
                if (_bufferPosition == 0) return 0f;
                
                var rms = _levelBuffer.Take(_bufferPosition)
                    .Select(x => x * x)
                    .Average();
                
                return Math.Min(1f, (float)Math.Sqrt(rms) * 2f);
            }
#else
            return 0f;
#endif
        }
    }

    /// <summary>
    /// Starts monitoring microphone input.
    /// </summary>
    public void StartMonitoring()
    {
#if WINDOWS
        if (_isMonitoring) return;

        try
        {
            // Create wave input device - WaveInEvent is better for background threads
            _waveIn = new WaveInEvent
            {
                DeviceNumber = 0, // Default device
                WaveFormat = new WaveFormat(44100, 16, 1) // 44.1kHz, 16-bit, Mono
            };

            // Hook up event handlers
            _waveIn.DataAvailable += OnWaveInDataAvailable;
            _waveIn.RecordingStopped += OnWaveInRecordingStopped;

            _isMonitoring = true;
            _waveIn.StartRecording();
        }
        catch (Exception ex)
        {
            _isMonitoring = false;
            MonitoringStopped?.Invoke(this, EventArgs.Empty);
            throw new InvalidOperationException("Failed to start microphone monitoring", ex);
        }
#endif
    }

    /// <summary>
    /// Stops monitoring microphone input.
    /// </summary>
    public void StopMonitoring()
    {
#if WINDOWS
        if (!_isMonitoring) return;

        try
        {
            _waveIn?.StopRecording();
            _isMonitoring = false;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to stop microphone monitoring", ex);
        }
#endif
    }

#if WINDOWS
    /// <summary>
    /// Handles incoming audio data from the microphone.
    /// </summary>
    private void OnWaveInDataAvailable(object? sender, WaveInEventArgs e)
    {
        // Convert byte buffer to float samples and calculate RMS
        lock (_levelLock)
        {
            _bufferPosition = 0;
            
            // Process samples from the audio buffer
            var bytesPerSample = 2; // 16-bit = 2 bytes
            for (int i = 0; i < e.BytesRecorded; i += bytesPerSample)
            {
                if (_bufferPosition >= _levelBuffer.Length) break;

                // Convert byte pair to short (16-bit signed)
                short sample = BitConverter.ToInt16(e.Buffer, i);
                
                // Normalize to -1 to 1 range
                _levelBuffer[_bufferPosition++] = sample / 32768f;
            }
        }

        // Raise level update event
        LevelUpdated?.Invoke(this, new AudioLevelEventArgs { Level = CurrentLevel });
    }

    /// <summary>
    /// Handles recording stopped event.
    /// </summary>
    private void OnWaveInRecordingStopped(object? sender, StoppedEventArgs e)
    {
        _isMonitoring = false;
        
        if (e.Exception != null)
        {
            System.Diagnostics.Debug.WriteLine($"Microphone error: {e.Exception.Message}");
        }

        MonitoringStopped?.Invoke(this, EventArgs.Empty);
    }
#endif

    /// <summary>
    /// Disposes the microphone monitoring service.
    /// </summary>
    public void Dispose()
    {
#if WINDOWS
        StopMonitoring();
        _waveIn?.Dispose();
#endif
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// Event arguments for audio level updates.
/// </summary>
public class AudioLevelEventArgs : EventArgs
{
    /// <summary>
    /// Gets or sets the current audio level (0.0 to 1.0).
    /// </summary>
    public float Level { get; set; }
}
