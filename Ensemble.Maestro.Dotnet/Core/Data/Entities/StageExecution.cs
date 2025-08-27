using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ensemble.Maestro.Dotnet.Core.Data.Entities;

/// <summary>
/// Represents the execution of a single stage within a pipeline (Planning, Designing, Swarming, Building, Validating)
/// </summary>
public class StageExecution
{
    /// <summary>
    /// Unique identifier for the stage execution
    /// </summary>
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// ID of the pipeline execution this stage belongs to
    /// </summary>
    [Required]
    public Guid PipelineExecutionId { get; set; }

    /// <summary>
    /// Stage name (Planning, Designing, Swarming, Building, Validating)
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string StageName { get; set; } = string.Empty;

    /// <summary>
    /// Current status of this stage execution
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = "Pending";

    /// <summary>
    /// Stage execution order within the pipeline
    /// </summary>
    public int ExecutionOrder { get; set; }

    /// <summary>
    /// When this stage started executing
    /// </summary>
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When this stage completed (if applicable)
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Duration of this stage in seconds
    /// </summary>
    public int? DurationSeconds { get; set; }

    /// <summary>
    /// Input data provided to this stage as JSON
    /// </summary>
    public string? InputData { get; set; }

    /// <summary>
    /// Output data produced by this stage as JSON
    /// </summary>
    public string? OutputData { get; set; }

    /// <summary>
    /// Configuration specific to this stage as JSON
    /// </summary>
    public string? StageConfig { get; set; }

    /// <summary>
    /// Number of items processed in this stage
    /// </summary>
    public int? ItemsProcessed { get; set; }

    /// <summary>
    /// Number of items completed successfully
    /// </summary>
    public int ItemsCompleted { get; set; } = 0;

    /// <summary>
    /// Number of items that failed processing
    /// </summary>
    public int ItemsFailed { get; set; } = 0;

    /// <summary>
    /// Progress percentage for this stage (0-100)
    /// </summary>
    public int ProgressPercentage { get; set; } = 0;

    /// <summary>
    /// Error message if the stage failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Stack trace if an exception occurred
    /// </summary>
    public string? ErrorStackTrace { get; set; }

    /// <summary>
    /// Detailed execution logs for this stage
    /// </summary>
    public string? ExecutionLogs { get; set; }

    /// <summary>
    /// Performance metrics for this stage as JSON
    /// </summary>
    public string? PerformanceMetrics { get; set; }

    /// <summary>
    /// Retry attempt number (0 for first attempt)
    /// </summary>
    public int RetryAttempt { get; set; } = 0;

    /// <summary>
    /// Maximum retry attempts allowed
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    // Navigation properties

    /// <summary>
    /// The pipeline execution this stage belongs to
    /// </summary>
    [ForeignKey(nameof(PipelineExecutionId))]
    public virtual PipelineExecution PipelineExecution { get; set; } = null!;

    /// <summary>
    /// Agent executions that occurred during this stage
    /// </summary>
    public virtual ICollection<AgentExecution> AgentExecutions { get; set; } = new List<AgentExecution>();

    // Computed properties

    /// <summary>
    /// Whether this stage has completed successfully
    /// </summary>
    [NotMapped]
    public bool IsCompleted => Status == "Completed" && CompletedAt.HasValue;

    /// <summary>
    /// Whether this stage has failed
    /// </summary>
    [NotMapped]
    public bool IsFailed => Status == "Failed" || Status == "Error";

    /// <summary>
    /// Whether this stage is currently executing
    /// </summary>
    [NotMapped]
    public bool IsExecuting => Status == "Running" || Status == "InProgress";

    /// <summary>
    /// Success rate as a percentage (0-100)
    /// </summary>
    [NotMapped]
    public double SuccessRate
    {
        get
        {
            if (ItemsProcessed == null || ItemsProcessed == 0) return 0;
            return (double)ItemsCompleted / ItemsProcessed.Value * 100;
        }
    }
}