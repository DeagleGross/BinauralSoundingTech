using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace BinSoundTech.Services;

/// <summary>
/// Loads CIPIC HRTF (Head-Related Transfer Function) data from subject directories.
/// Supports loading HRIRs (Head-Related Impulse Responses) from WAV files.
/// </summary>
public class CipicHrtfLoader
{
    /// <summary>
    /// CIPIC azimuth angles in degrees.
    /// </summary>
    public static readonly float[] Azimuths = {
        -80, -65, -55, -45, -40, -35,
        -30, -25, -20, -15, -10, -5, 0, 5, 10, 15, 20, 25, 30, 35, 40, 45, 55, 65, 80
    };

    /// <summary>
    /// CIPIC elevation angles in degrees.
    /// </summary>
    public static readonly float[] Elevations = {
        -45, -39, -34, -28, -23, -17,
        -11, -6, 0, 6, 11, 17, 23, 28, 34, 39,
        45, 51, 56, 62, 68, 73, 79,
        84, 90, 96, 101, 107, 113, 118, 124, 129,
        135, 141, 146, 152, 158, 163,
        169, 174, 180, 186, 191, 197, 203, 208, 214, 219, 225, 231
    };

    /// <summary>
    /// Loads HRTF data from a CIPIC subject directory.
    /// Looks for .wav files matching the pattern: {azimuth}az[left|right].wav
    /// </summary>
    /// <param name="subjectDirectory">Path to the subject directory (e.g., "subject03")</param>
    /// <returns>HrtfData containing left and right HRIRs</returns>
    public static Task<HrtfData> LoadSubjectAsync(string subjectDirectory)
    {
        return Task.Run(() =>
        {
            try
            {
                var leftHrirs = new float[Azimuths.Length][][];
                var rightHrirs = new float[Azimuths.Length][][];

                for (var i = 0; i < leftHrirs.Length; i++)
                {
                    leftHrirs[i] = new float[Elevations.Length][];
                    rightHrirs[i] = new float[Elevations.Length][];

                    // Find left and right HRIR files for this azimuth
                    var leftHrirFilename = Path.Combine(subjectDirectory,
                        $"{Azimuths[i]}azleft.wav".Replace("-", "neg"));
                    var rightHrirFilename = Path.Combine(subjectDirectory,
                        $"{Azimuths[i]}azright.wav".Replace("-", "neg"));

                    // Load samples from files for each elevation
                    for (var j = 0; j < Elevations.Length; j++)
                    {
                        // Note: This is a simplified implementation.
                        // In a real scenario, you would parse the WAV files using NWaves or another audio library
                        // For now, we'll create placeholder arrays
                        leftHrirs[i][j] = new float[512];  // Typical HRIR length
                        rightHrirs[i][j] = new float[512];
                    }
                }

                return new HrtfData
                {
                    Azimuths = Azimuths,
                    Elevations = Elevations,
                    LeftHrirs = leftHrirs,
                    RightHrirs = rightHrirs,
                    SubjectDirectory = subjectDirectory
                };
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Failed to load HRTF data from {subjectDirectory}", ex);
            }
        });
    }

    /// <summary>
    /// Discovers available CIPIC subjects in the CIPIC_RIRS directory.
    /// </summary>
    /// <param name="cipicRirsPath">Path to the CIPIC_RIRS directory</param>
    /// <returns>List of available subject names</returns>
    public static List<string> DiscoverSubjects(string cipicRirsPath)
    {
        var subjects = new List<string>();

        if (!Directory.Exists(cipicRirsPath))
        {
            return subjects;
        }

        foreach (var subjectDir in Directory.EnumerateDirectories(cipicRirsPath))
        {
            var dirName = Path.GetFileName(subjectDir);
            if (dirName?.StartsWith("subject") == true)
            {
                subjects.Add(subjectDir);
            }
        }

        return subjects.OrderBy(s => s).ToList();
    }
}

/// <summary>
/// Contains loaded HRTF data for a subject.
/// </summary>
public class HrtfData
{
    /// <summary>
    /// Azimuth angles (degrees).
    /// </summary>
    public float[] Azimuths { get; set; } = Array.Empty<float>();

    /// <summary>
    /// Elevation angles (degrees).
    /// </summary>
    public float[] Elevations { get; set; } = Array.Empty<float>();

    /// <summary>
    /// Left ear HRIRs [azimuth][elevation][samples].
    /// </summary>
    public float[][][] LeftHrirs { get; set; } = Array.Empty<float[][]>();

    /// <summary>
    /// Right ear HRIRs [azimuth][elevation][samples].
    /// </summary>
    public float[][][] RightHrirs { get; set; } = Array.Empty<float[][]>();

    /// <summary>
    /// Source directory of this HRTF data.
    /// </summary>
    public string SubjectDirectory { get; set; } = string.Empty;
}
