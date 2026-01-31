namespace BinSoundTech.Services;

/// <summary>
/// Helper class for locating the CIPIC HRTF data directory.
/// </summary>
public static class CipicPathResolver
{
    private static string? CipicRirsDirectory;

    /// <summary>
    /// Finds the CIPIC_RIRS directory by searching various locations.
    /// Searches up from AppContext.BaseDirectory and Environment.CurrentDirectory
    /// to find the repository root where CIPIC_RIRS is located.
    /// </summary>
    /// <returns>The full path to the CIPIC_RIRS directory, or null if not found.</returns>
    public static string? FindCipicDirectory()
    {
        if (CipicRirsDirectory is not null)
        {
            return CipicRirsDirectory;
        }

        var searchPaths = new List<string>();
        
        // From AppContext.BaseDirectory (bin/Debug/net10.0-windows...)
        // Go up to find repository root
        var baseDir = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar);
        
        // Try going up various levels from the bin output directory
        var current = baseDir;
        for (int i = 0; i < 10; i++)
        {
            var parent = Path.GetDirectoryName(current);
            if (string.IsNullOrEmpty(parent)) break;
            
            searchPaths.Add(Path.Combine(parent, "CIPIC_RIRS"));
            current = parent;
        }
        
        // Also try from current directory
        searchPaths.Add(Path.Combine(Environment.CurrentDirectory, "CIPIC_RIRS"));
        
        // Try from current directory going up
        current = Environment.CurrentDirectory;
        for (int i = 0; i < 8; i++)
        {
            var parent = Path.GetDirectoryName(current);
            if (string.IsNullOrEmpty(parent)) break;
            
            searchPaths.Add(Path.Combine(parent, "CIPIC_RIRS"));
            current = parent;
        }

        // Also check some common development paths
        // When running from Visual Studio, the working directory might be the project folder
        searchPaths.Add(Path.Combine(baseDir, "CIPIC_RIRS"));
        searchPaths.Add(Path.Combine(baseDir, "..", "CIPIC_RIRS"));
        searchPaths.Add(Path.Combine(baseDir, "..", "..", "CIPIC_RIRS"));

        // Search for the directory
        foreach (var path in searchPaths)
        {
            try
            {
                var normalizedPath = Path.GetFullPath(path);
                if (Directory.Exists(normalizedPath))
                {
                    // Verify it contains HRTF data by checking for subdirectories
                    var subdirs = Directory.GetDirectories(normalizedPath, "subject*");
                    if (subdirs.Length > 0)
                    {
#if DEBUG
                        System.Diagnostics.Debug.WriteLine($"Found CIPIC_RIRS at: {normalizedPath}");
#endif
                        CipicRirsDirectory = normalizedPath;
                        return normalizedPath;
                    }
                }
            }
            catch
            {
                // Ignore invalid paths
            }
        }

#if DEBUG
        System.Diagnostics.Debug.WriteLine($"CIPIC_RIRS not found. Searched locations:");
        System.Diagnostics.Debug.WriteLine($"  AppContext.BaseDirectory: {AppContext.BaseDirectory}");
        System.Diagnostics.Debug.WriteLine($"  Environment.CurrentDirectory: {Environment.CurrentDirectory}");
#endif

        return null;
    }
}
