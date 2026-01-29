#if WINDOWS
using NAudio.Wave;
using NAudioWaveFormat = NAudio.Wave.WaveFormat;
#endif
using NWaves.Effects.Stereo;

namespace BinSoundTech.Services;

/// <summary>
/// Audio playback service with binaural panning support.
/// Uses NAudio for Windows playback and NWaves for audio processing and effects.
/// Based on the NWaves.DemoStereo sample.
/// </summary>
public class AudioPlaybackService : IDisposable
#if WINDOWS
    , ISampleProvider
#endif
{
#if WINDOWS
    private AudioFileReader? _reader;
    private WaveOutEvent? _player;
    private StereoEffect? _effect;
    private readonly float[] _tmpBuffer = new float[16000];
    private NAudioWaveFormat? _waveFormat;
#endif

    private bool _isPlaying;
    private float _azimuth;
    private float _elevation;
    private float _targetAzimuth;
    private float _targetElevation;
    private float _azimuthRampRemaining;
    private float _elevationRampRemaining;
    private const float RampDurationMs = 50f; // 50ms ramp for smooth transitions
    private BinauralPanEffect? _binauralPanEffect;
    private PanEffect _panEffect = new(0, PanRule.Linear);

    /// <summary>
    /// Gets whether audio is currently playing.
    /// </summary>
    public bool IsPlaying => _isPlaying;

    /// <summary>
    /// Gets or sets the azimuth angle in degrees (-80 to 80).
    /// Changes are ramped smoothly to avoid audio clicks.
    /// </summary>
    public float Azimuth
    {
        get => _azimuth;
        set
        {
            if (!_isPlaying)
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
            if (!_isPlaying)
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
    /// Gets or sets the pan value (-1 to 1) for simple stereo panning.
    /// </summary>
    public float Pan
    {
        get => _panEffect.Pan;
        set => _panEffect.Pan = value;
    }

    /// <summary>
    /// Gets the number of channels in the loaded audio.
    /// </summary>
    public int Channels { get; private set; }

#if WINDOWS
    /// <summary>
    /// Gets the wave format of the audio output.
    /// </summary>
    public NAudioWaveFormat? WaveFormat => _waveFormat;
#endif

    /// <summary>
    /// Event raised when playback completes.
    /// </summary>
    public event EventHandler? PlaybackCompleted;

    /// <summary>
    /// Loads HRTF data for binaural panning effect.
    /// </summary>
    /// <param name="hrtfData">The HRTF data to use</param>
    public void LoadHrtfData(HrtfData hrtfData)
    {
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
    }

    /// <summary>
    /// Loads an audio file for playback.
    /// </summary>
    /// <param name="filePath">Path to the audio file</param>
    public void Load(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Audio file not found: {filePath}");
        }

#if WINDOWS
        Stop();
        _player?.Dispose();
        _reader?.Dispose();

        _reader = new AudioFileReader(filePath);
        Channels = _reader.WaveFormat.Channels;
        
        // Output is always stereo for binaural effect
        _waveFormat = NAudioWaveFormat.CreateIeeeFloatWaveFormat(_reader.WaveFormat.SampleRate, 2);

        _player = new WaveOutEvent();
        _player.Init(this);
        _player.PlaybackStopped += OnPlaybackStopped;
#endif
    }

#if WINDOWS
    private void OnPlaybackStopped(object? sender, StoppedEventArgs e)
    {
        _isPlaying = false;
        PlaybackCompleted?.Invoke(this, EventArgs.Empty);
    }
#endif

    /// <summary>
    /// Updates the stereo effect to use for playback.
    /// </summary>
    /// <param name="effect">The stereo effect to apply</param>
    public void SetEffect(StereoEffect effect)
    {
#if WINDOWS
        _effect = effect;
#endif
    }

    /// <summary>
    /// Starts or resumes playback with the current effect.
    /// If binaural HRTF is loaded, uses binaural panning; otherwise uses simple stereo panning.
    /// </summary>
    public void Play()
    {
#if WINDOWS
        if (_player == null) return;

        // Choose effect: binaural if HRTF loaded, otherwise simple pan
        _effect = _binauralPanEffect ?? (StereoEffect)_panEffect;

        if (_player.PlaybackState == PlaybackState.Stopped)
        {
            _reader?.Seek(0, SeekOrigin.Begin);
        }

        _player.Play();
        _isPlaying = true;
#endif
    }

    /// <summary>
    /// Pauses playback.
    /// </summary>
    public void Pause()
    {
#if WINDOWS
        _player?.Pause();
        _isPlaying = false;
#endif
    }

    /// <summary>
    /// Stops playback and resets to beginning.
    /// </summary>
    public void Stop()
    {
#if WINDOWS
        _player?.Stop();
        _reader?.Seek(0, SeekOrigin.Begin);
        _isPlaying = false;
#endif
    }

#if WINDOWS
    /// <summary>
    /// ISampleProvider implementation - reads samples and applies stereo effect.
    /// </summary>
    public int Read(float[] buffer, int offset, int count)
    {
        if (_reader == null) return 0;

        return _reader.WaveFormat.Channels switch
        {
            1 => ReadMono(buffer, offset, count),
            _ => ReadStereo(buffer, offset, count),
        };
    }

    /// <summary>
    /// Reads mono audio and applies stereo effect to create binaural output.
    /// Handles smooth parameter ramping to avoid clicks.
    /// </summary>
    private int ReadMono(float[] buffer, int offset, int count)
    {
        if (_reader == null) return 0;

        // Read mono samples (half the count since we output stereo)
        var samplesRead = _reader.Read(_tmpBuffer, 0, count / 2);

        if (_effect == null || samplesRead == 0)
        {
            return samplesRead;
        }

        var sampleRateHz = _reader.WaveFormat.SampleRate;

        // Apply stereo effect to mono input, producing stereo output
        var pos = offset;
        for (var n = 0; n < samplesRead; n++)
        {
            // Update ramping parameters
            if (_azimuthRampRemaining > 0 && _binauralPanEffect != null)
            {
                var rampProgress = 1f - (_azimuthRampRemaining / RampDurationMs);
                _azimuth = _azimuth + (_targetAzimuth - _azimuth) * rampProgress;
                _binauralPanEffect.Azimuth = _azimuth;
                _azimuthRampRemaining -= (1000f / sampleRateHz);
            }

            if (_elevationRampRemaining > 0 && _binauralPanEffect != null)
            {
                var rampProgress = 1f - (_elevationRampRemaining / RampDurationMs);
                _elevation = _elevation + (_targetElevation - _elevation) * rampProgress;
                _binauralPanEffect.Elevation = _elevation;
                _elevationRampRemaining -= (1000f / sampleRateHz);
            }

            _effect.Process(_tmpBuffer[n], out float left, out float right);
            buffer[pos++] = left;
            buffer[pos++] = right;
        }

        return samplesRead * 2;
    }

    /// <summary>
    /// Reads stereo audio and applies stereo effect.
    /// Handles smooth parameter ramping to avoid clicks.
    /// </summary>
    private int ReadStereo(float[] buffer, int offset, int count)
    {
        if (_reader == null) return 0;

        var samplesRead = _reader.Read(buffer, offset, count);

        if (_effect == null || samplesRead == 0)
        {
            return samplesRead;
        }

        var sampleRateHz = _reader.WaveFormat.SampleRate;

        // Apply stereo effect to each stereo sample pair
        for (var n = offset; n < samplesRead; n += 2)
        {
            // Update ramping parameters
            if (_azimuthRampRemaining > 0 && _binauralPanEffect != null)
            {
                var rampProgress = 1f - (_azimuthRampRemaining / RampDurationMs);
                _azimuth = _azimuth + (_targetAzimuth - _azimuth) * rampProgress;
                _binauralPanEffect.Azimuth = _azimuth;
                _azimuthRampRemaining -= (1000f / sampleRateHz);
            }

            if (_elevationRampRemaining > 0 && _binauralPanEffect != null)
            {
                var rampProgress = 1f - (_elevationRampRemaining / RampDurationMs);
                _elevation = _elevation + (_targetElevation - _elevation) * rampProgress;
                _binauralPanEffect.Elevation = _elevation;
                _elevationRampRemaining -= (1000f / sampleRateHz);
            }

            _effect.Process(ref buffer[n], ref buffer[n + 1]);
        }

        return samplesRead;
    }
#endif

    /// <summary>
    /// Disposes resources.
    /// </summary>
    public void Dispose()
    {
#if WINDOWS
        _player?.Stop();
        _player?.Dispose();
        _reader?.Dispose();
#endif
        GC.SuppressFinalize(this);
    }
}
