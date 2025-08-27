namespace Ensemble.Maestro.Dotnet.Core.Services;

/// <summary>
/// Service interface for executing actual build operations on aggregated code files
/// </summary>
public interface IBuildExecutionService
{
    /// <summary>
    /// Execute a build operation in the specified directory
    /// </summary>
    Task<BuildExecutionResult> ExecuteBuildAsync(string buildDirectory, string primaryLanguage, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate build prerequisites for a given language
    /// </summary>
    Task<BuildValidationResult> ValidateBuildPrerequisitesAsync(string primaryLanguage, 
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of a build execution operation
/// </summary>
public class BuildExecutionResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string BuildOutput { get; set; } = string.Empty;
    public List<BuildError> ErrorDetails { get; set; } = new();
    public List<string> GeneratedArtifacts { get; set; } = new();
    public int BuildDurationSeconds { get; set; }
    public DateTime BuildStarted { get; set; } = DateTime.UtcNow;
    public DateTime BuildCompleted { get; set; }
}

/// <summary>
/// Represents a build error with detailed information for bug-fix agent spawning
/// </summary>
public class BuildError
{
    public string ErrorType { get; set; } = string.Empty; // CompileError, RuntimeError, etc.
    public string ErrorMessage { get; set; } = string.Empty;
    public string? Details { get; set; }
    public string? StackTrace { get; set; }
    public string? FileName { get; set; }
    public string? FunctionName { get; set; }
    public string? FunctionSignature { get; set; }
    public int? LineNumber { get; set; }
    public string? CodeUnitName { get; set; }
    public int Severity { get; set; } = 5; // 1-10 scale
    public string? SuggestedFix { get; set; }
    public List<string> RelatedFunctions { get; set; } = new();
}

/// <summary>
/// Result of build prerequisite validation
/// </summary>
public class BuildValidationResult
{
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
    public List<string> MissingPrerequisites { get; set; } = new();
    public List<string> AvailableTools { get; set; } = new();
}