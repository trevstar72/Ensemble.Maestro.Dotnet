namespace Ensemble.Maestro.Dotnet.Core.Configuration;

/// <summary>
/// Configuration for output paths with centralized root directory
/// All output paths are relative to RootDirectory with hardcoded subfolders
/// </summary>
public class OutputPathsConfiguration
{
    public const string SectionName = "OutputPaths";
    
    /// <summary>
    /// Root directory for all Maestro outputs (e.g., "D:\maestro")
    /// </summary>
    public string RootDirectory { get; set; } = "D:\\maestro";
    
    /// <summary>
    /// Gets the AI agent outputs directory path
    /// </summary>
    public string AgentOutputsDirectory => Path.Combine(RootDirectory, "ai-outputs");
    
    /// <summary>
    /// Gets the logs directory path
    /// </summary>
    public string LogsDirectory => Path.Combine(RootDirectory, "logs");
    
    /// <summary>
    /// Gets the temporary files directory path
    /// </summary>
    public string TempDirectory => Path.Combine(RootDirectory, "temp");
    
    /// <summary>
    /// Gets the exports directory path
    /// </summary>
    public string ExportsDirectory => Path.Combine(RootDirectory, "exports");
    
    /// <summary>
    /// Gets the code documents directory path
    /// </summary>
    public string DocumentsDirectory => Path.Combine(RootDirectory, "documents");
    
    /// <summary>
    /// Gets the build outputs directory path
    /// </summary>
    public string BuildsDirectory => Path.Combine(RootDirectory, "builds");

    /// <summary>
    /// Ensures all required directories exist
    /// </summary>
    public void EnsureDirectoriesExist()
    {
        var directories = new[]
        {
            RootDirectory,
            AgentOutputsDirectory,
            LogsDirectory,
            TempDirectory,
            ExportsDirectory,
            DocumentsDirectory,
            BuildsDirectory
        };

        foreach (var directory in directories)
        {
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }
    }
}