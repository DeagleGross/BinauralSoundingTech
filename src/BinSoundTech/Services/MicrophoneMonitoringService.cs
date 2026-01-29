#if WINDOWS
using NAudio.Wave;
using NAudioWaveFormat = NAudio.Wave.WaveFormat;
#endif
using NWaves.Effects.Stereo;

namespace BinSoundTech.Services;

/// <summary>
/// Microphone monitoring service that captures live microphone input with binaural panning.
/// Uses NAudio for Windows audio capture and applies real-time binaural HRTF processing.
/// </summary>
public class MicrophoneMonitoringService : IDisposable
#if WINDOWS
    , ISampleProvider
#endif
{
#if WINDOWS
    private IWaveIn? _waveIn;
    private IWavePlayer? _waveOut;
    private BufferedWaveProvider? _bufferedProvider;
    private NAudioWaveFormat? _waveFormat;
    private volatile bool _isMonitoring;
    private float[] _levelBuffer = new float[2048];
    private int _bufferPosition;
    private readonly object _levelLock = new();
    private BinauralPanEffect? _binauralPanEffect;
    private BinauralPanEffect? _binauralPanEffectNext;
    private float _azimuth;
    private float _elevation;
    private float _targetAzimuth;
    private float _targetElevation;
    private float _azimuthRampRemaining;
    private float _elevationRampRemaining;
    private const float RampDurationMs = 50f; // 50ms ramp for smooth transitions
    private readonly float[] _tmpBuffer = new float[16000];
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
    /// Gets or sets the azimuth angle in degrees (-80 to 80).
    /// Changes are ramped smoothly to avoid audio clicks.
    /// </summary>
    public float Azimuth
    {
        get => _azimuth;
        set
        {
            if (!_isMonitoring) 
            {
                _azimuth = value;
                if (_binauralPanEffect != null)
                {
                    _binauralPanEffect.Azimuth = value;
                }
                return;
            }

            // If value changed, initiate ramp
            if (Math.Abs(value - _targetAzimuth) > 0.1f)
            {
                _targetAzimuth = value;
                _azimuthRampRemaining = RampDurationMs;
            }
        }
    }

    /// <summary>
    /// Gets or sets the elevation angle in degrees (-45 to 231).
    /// Changes are ramped smoothly to avoid audio clicks.
    /// </summary>
    public float Elevation
    {
        get => _elevation;
        set
        {
            if (!_isMonitoring)
            {
                _elevation = value;
                if (_binauralPanEffect != null)
                {
                    _binauralPanEffect.Elevation = value;
                }
                return;
            }

            // If value changed, initiate ramp
            if (Math.Abs(value - _targetElevation) > 0.1f)
            {
                _targetElevation = value;
                _elevationRampRemaining = RampDurationMs;
            }
        }
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
    /// Loads HRTF data for binaural panning effect.
    /// </summary>
    /// <param name="hrtfData">The HRTF data to use</param>
    public void LoadHrtfData(HrtfData hrtfData)
    {
#if WINDOWS
        if (hrtfData.LeftHrirs.Length == 0 || hrtfData.RightHrirs.Length == 0)
        {
            throw new InvalidOperationException("HRTF data is empty");
        }

        _binauralPanEffect = new BinauralPanEffect(
            hrtfData.Azimuths,
            hrtfData.Elevations,
            hrtfData.LeftHrirs,
            hrtfData.RightHrirs)
        {
            Azimuth = _azimuth,
            Elevation = _elevation
        };

        // Initialize target values to current values (no ramping on load)
        _targetAzimuth = _azimuth;
        _targetElevation = _elevation;
        _azimuthRampRemaining = 0;
        _elevationRampRemaining = 0;
#endif
    }

    /// <summary>
    /// Starts monitoring microphone input with binaural panning applied.
    /// </summary>
    public void StartMonitoring()
    {
#if WINDOWS
        if (_isMonitoring) return;

        try
        {
            // Create wave format for microphone input - 44.1kHz, 16-bit, Mono
            var inputFormat = new NAudioWaveFormat(44100, 16, 1);

            // Create wave input device
            _waveIn = new WaveInEvent
            {
                DeviceNumber = 0, // Default device
                WaveFormat = inputFormat
            };

            // Create buffered provider for processed audio output (stereo, IEEE float)
            _waveFormat = NAudioWaveFormat.CreateIeeeFloatWaveFormat(44100, 2);
            _bufferedProvider = new BufferedWaveProvider(_waveFormat)
            {
                DiscardOnBufferOverflow = true,
                BufferDuration = TimeSpan.FromMilliseconds(200)
            };

            // Create wave output device
            _waveOut = new WaveOutEvent();
            _waveOut.Init(_bufferedProvider);
            _waveOut.Play();

            // Hook up event handlers
            _waveIn.DataAvailable += OnWaveInDataAvailable;
            _waveIn.RecordingStopped += OnWaveInRecordingStopped;

            _isMonitoring = true;
            _waveIn.StartRecording();
        }
        catch (Exception ex)
        {
            _isMonitoring = false;
            Cleanup();
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
            Cleanup();
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
        // Convert byte buffer to float samples
        var bytesPerSample = 2; // 16-bit = 2 bytes
        var sampleCount = e.BytesRecorded / bytesPerSample;
        var floatBuffer = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            short sample = BitConverter.ToInt16(e.Buffer, i * bytesPerSample);
            floatBuffer[i] = sample / 32768f;
        }

        // Apply binaural panning effect if available
        byte[] processedAudio;
        if (_binauralPanEffect != null)
        {
            processedAudio = ProcessAudioWithBinaural(floatBuffer);
        }
        else
        {
            // No binaural effect, just convert mono to stereo
            processedAudio = ConvertMonoToStereo(floatBuffer);
        }

        // Add processed audio to playback buffer
        _bufferedProvider?.AddSamples(processedAudio, 0, processedAudio.Length);

        // Update level monitoring
        UpdateLevelMonitoring(floatBuffer);

        // Raise level update event
        LevelUpdated?.Invoke(this, new AudioLevelEventArgs { Level = CurrentLevel });
    }

    /// <summary>
    /// Processes mono audio through the binaural panning effect to create stereo output.
    /// Handles smooth parameter ramping to avoid clicks.
    /// </summary>
    private byte[] ProcessAudioWithBinaural(float[] monoSamples)
    {
        var stereoSamples = new float[monoSamples.Length * 2];
        var sampleRateHz = 44100f;
        
        for (int i = 0; i < monoSamples.Length; i++)
        {
            // Update ramping parameters
            if (_azimuthRampRemaining > 0)
            {
                var rampProgress = 1f - (_azimuthRampRemaining / RampDurationMs);
                _azimuth = _azimuth + (_targetAzimuth - _azimuth) * rampProgress;
                _binauralPanEffect!.Azimuth = _azimuth;
                _azimuthRampRemaining -= (1000f / sampleRateHz); // Convert to ms
            }

            if (_elevationRampRemaining > 0)
            {
                var rampProgress = 1f - (_elevationRampRemaining / RampDurationMs);
                _elevation = _elevation + (_targetElevation - _elevation) * rampProgress;
                _binauralPanEffect!.Elevation = _elevation;
                _elevationRampRemaining -= (1000f / sampleRateHz); // Convert to ms
            }

            _binauralPanEffect!.Process(monoSamples[i], out float left, out float right);
            stereoSamples[i * 2] = left;
            stereoSamples[i * 2 + 1] = right;
        }

        return ConvertFloatToBytes(stereoSamples);
    }

    /// <summary>
    /// Converts mono audio to stereo without processing.
    /// </summary>
    private byte[] ConvertMonoToStereo(float[] monoSamples)
    {
        var stereoSamples = new float[monoSamples.Length * 2];
        
        for (int i = 0; i < monoSamples.Length; i++)
        {
            // Duplicate mono sample to both channels
            stereoSamples[i * 2] = monoSamples[i];
            stereoSamples[i * 2 + 1] = monoSamples[i];
        }

        return ConvertFloatToBytes(stereoSamples);
    }

    /// <summary>
    /// Converts float samples to byte buffer (IEEE 32-bit float format).
    /// </summary>
    private byte[] ConvertFloatToBytes(float[] samples)
    {
        var bytes = new byte[samples.Length * 4];
        Buffer.BlockCopy(samples, 0, bytes, 0, bytes.Length);
        return bytes;
    }

    /// <summary>
    /// Updates level monitoring from audio samples.
    /// </summary>
    private void UpdateLevelMonitoring(float[] samples)
    {
        lock (_levelLock)
        {
            _bufferPosition = Math.Min(samples.Length, _levelBuffer.Length);
            Array.Copy(samples, _levelBuffer, _bufferPosition);
        }
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

    /// <summary>
    /// Cleans up audio playback resources.
    /// </summary>
    private void Cleanup()
    {
        _waveOut?.Stop();
        _waveOut?.Dispose();
        _waveOut = null;
        _bufferedProvider = null;
    }

    /// <summary>
    /// ISampleProvider implementation.
    /// </summary>
    public NAudioWaveFormat WaveFormat => _waveFormat ?? NAudioWaveFormat.CreateIeeeFloatWaveFormat(44100, 2);

    /// <summary>
    /// ISampleProvider implementation.
    /// </summary>
    public int Read(float[] buffer, int offset, int count)
    {
        // This is not used in current implementation
        return 0;
    }
#endif

    /// <summary>
    /// Disposes the microphone monitoring service.
    /// </summary>
    public void Dispose()
    {
#if WINDOWS
        StopMonitoring();
        Cleanup();
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

