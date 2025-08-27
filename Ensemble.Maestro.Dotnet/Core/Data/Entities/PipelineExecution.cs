using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ensemble.Maestro.Dotnet.Core.Data.Entities;

/// <summary>
/// Represents a single execution of the Maestro pipeline (Planning → Designing → Swarming → Building → Validating)
/// </summary>
public class PipelineExecution
{
    /// <summary>
    /// Unique identifier for the pipeline execution
    /// </summary>
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// ID of the project this pipeline execution belongs to
    /// </summary>
    [Required]
    public Guid ProjectId { get; set; }

    /// <summary>
    /// Current stage of the pipeline
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Stage { get; set; } = "Pending";

    /// <summary>
    /// Current status of the pipeline execution
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = "Pending";

    /// <summary>
    /// Template ID used for this execution (if any)
    /// </summary>
    public Guid? TemplateId { get; set; }

    /// <summary>
    /// When this pipeline execution started
    /// </summary>
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the current stage started
    /// </summary>
    public DateTime StageStartedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When this pipeline execution completed (if applicable)
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Target programming language for this execution
    /// </summary>
    [MaxLength(100)]
    public string? TargetLanguage { get; set; }

    /// <summary>
    /// Target deployment environment
    /// </summary>
    [MaxLength(100)]
    public string? DeploymentTarget { get; set; }

    /// <summary>
    /// Number of agents in the pool for this execution
    /// </summary>
    public int? AgentPoolSize { get; set; }

    /// <summary>
    /// Total estimated functions to be processed
    /// </summary>
    public int? TotalFunctions { get; set; }

    /// <summary>
    /// Number of functions completed successfully
    /// </summary>
    public int CompletedFunctions { get; set; } = 0;

    /// <summary>
    /// Number of functions that failed processing
    /// </summary>
    public int FailedFunctions { get; set; } = 0;

    /// <summary>
    /// Estimated duration in seconds
    /// </summary>
    public int? EstimatedDurationSeconds { get; set; }

    /// <summary>
    /// Actual duration in seconds (when completed)
    /// </summary>
    public int? ActualDurationSeconds { get; set; }

    /// <summary>
    /// Overall progress percentage (0-100)
    /// </summary>
    public int ProgressPercentage { get; set; } = 0;

    /// <summary>
    /// Error message if the pipeline failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Execution logs as JSON
    /// </summary>
    public string? ExecutionLogs { get; set; }

    /// <summary>
    /// Execution metrics as JSON
    /// </summary>
    public string? ExecutionMetrics { get; set; }

    /// <summary>
    /// Configuration used for this execution as JSON
    /// </summary>
    public string? ExecutionConfig { get; set; }

    // Navigation properties

    /// <summary>
    /// The project this pipeline execution belongs to
    /// </summary>
    [ForeignKey(nameof(ProjectId))]
    public virtual Project Project { get; set; } = null!;

    /// <summary>
    /// Individual stage executions within this pipeline
    /// </summary>
    public virtual ICollection<StageExecution> StageExecutions { get; set; } = new List<StageExecution>();

    /// <summary>
    /// Agent executions that are part of this pipeline
    /// </summary>
    public virtual ICollection<AgentExecution> AgentExecutions { get; set; } = new List<AgentExecution>();

    /// <summary>
    /// Orchestration results from this pipeline execution
    /// </summary>
    public virtual ICollection<OrchestrationResult> OrchestrationResults { get; set; } = new List<OrchestrationResult>();
}