using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ensemble.Maestro.Dotnet.Core.Data.Entities;

/// <summary>
/// Represents the result of a Semantic Kernel orchestration (GroupChat, Sequential, Handoff, etc.)
/// </summary>
public class OrchestrationResult
{
    /// <summary>
    /// Unique identifier for the orchestration result
    /// </summary>
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// ID of the pipeline execution this result belongs to
    /// </summary>
    [Required]
    public Guid PipelineExecutionId { get; set; }

    /// <summary>
    /// Type of orchestration pattern used
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string OrchestrationPattern { get; set; } = string.Empty;

    /// <summary>
    /// Specific orchestration name or identifier
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string OrchestrationName { get; set; } = string.Empty;

    /// <summary>
    /// Current status of the orchestration
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = "Pending";

    /// <summary>
    /// When the orchestration started
    /// </summary>
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the orchestration completed (if applicable)
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Duration of the orchestration in seconds
    /// </summary>
    public int? DurationSeconds { get; set; }

    /// <summary>
    /// Initial input/task provided to the orchestration
    /// </summary>
    [Required]
    public string InitialInput { get; set; } = string.Empty;

    /// <summary>
    /// Final output/result from the orchestration
    /// </summary>
    public string? FinalOutput { get; set; }

    /// <summary>
    /// Number of agents involved in this orchestration
    /// </summary>
    public int AgentCount { get; set; } = 0;

    /// <summary>
    /// Number of messages exchanged during orchestration
    /// </summary>
    public int MessageCount { get; set; } = 0;

    /// <summary>
    /// Number of function calls made during orchestration
    /// </summary>
    public int FunctionCallCount { get; set; } = 0;

    /// <summary>
    /// Total tokens consumed by all agents
    /// </summary>
    public int? TotalTokens { get; set; }

    /// <summary>
    /// Total cost of the orchestration in USD
    /// </summary>
    [Column(TypeName = "decimal(10,6)")]
    public decimal? TotalCost { get; set; }

    /// <summary>
    /// Success rate as percentage (0-100)
    /// </summary>
    public int SuccessRate { get; set; } = 0;

    /// <summary>
    /// Quality score of the final output (0-100)
    /// </summary>
    public int? QualityScore { get; set; }

    /// <summary>
    /// Confidence score of the final output (0-100)
    /// </summary>
    public int? ConfidenceScore { get; set; }

    /// <summary>
    /// Agents involved in this orchestration as JSON array
    /// </summary>
    public string? ParticipatingAgents { get; set; }

    /// <summary>
    /// Orchestration configuration used as JSON
    /// </summary>
    public string? OrchestrationConfig { get; set; }

    /// <summary>
    /// Runtime configuration as JSON
    /// </summary>
    public string? RuntimeConfig { get; set; }

    /// <summary>
    /// Detailed execution flow/timeline as JSON
    /// </summary>
    public string? ExecutionFlow { get; set; }

    /// <summary>
    /// Performance metrics as JSON
    /// </summary>
    public string? PerformanceMetrics { get; set; }

    /// <summary>
    /// Error message if the orchestration failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Stack trace if an exception occurred
    /// </summary>
    public string? ErrorStackTrace { get; set; }

    /// <summary>
    /// Detailed execution logs
    /// </summary>
    public string? ExecutionLogs { get; set; }

    /// <summary>
    /// Warnings encountered during execution
    /// </summary>
    public string? Warnings { get; set; }

    /// <summary>
    /// Retry attempt number (0 for first attempt)
    /// </summary>
    public int RetryAttempt { get; set; } = 0;

    /// <summary>
    /// Maximum retry attempts allowed
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Cancellation token used (for tracking cancellations)
    /// </summary>
    public string? CancellationToken { get; set; }

    /// <summary>
    /// Whether the orchestration was cancelled
    /// </summary>
    public bool WasCancelled { get; set; } = false;

    /// <summary>
    /// Timeout setting in seconds
    /// </summary>
    public int? TimeoutSeconds { get; set; }

    /// <summary>
    /// Whether the orchestration timed out
    /// </summary>
    public bool DidTimeout { get; set; } = false;

    /// <summary>
    /// Priority level of this orchestration
    /// </summary>
    [MaxLength(20)]
    public string Priority { get; set; } = "Medium";

    /// <summary>
    /// Metadata about the orchestration as JSON
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// Tags for categorization
    /// </summary>
    public string? Tags { get; set; }

    /// <summary>
    /// Parent orchestration result ID (for nested orchestrations)
    /// </summary>
    public Guid? ParentOrchestrationId { get; set; }

    /// <summary>
    /// Context data passed to the orchestration as JSON
    /// </summary>
    public string? ContextData { get; set; }

    /// <summary>
    /// Input transformations applied as JSON
    /// </summary>
    public string? InputTransformations { get; set; }

    /// <summary>
    /// Output transformations applied as JSON
    /// </summary>
    public string? OutputTransformations { get; set; }

    // Navigation properties

    /// <summary>
    /// The pipeline execution this result belongs to
    /// </summary>
    [ForeignKey(nameof(PipelineExecutionId))]
    public virtual PipelineExecution PipelineExecution { get; set; } = null!;

    /// <summary>
    /// Parent orchestration result (for nested orchestrations)
    /// </summary>
    [ForeignKey(nameof(ParentOrchestrationId))]
    public virtual OrchestrationResult? ParentOrchestration { get; set; }

    /// <summary>
    /// Child orchestration results (for nested orchestrations)
    /// </summary>
    public virtual ICollection<OrchestrationResult> ChildOrchestrations { get; set; } = new List<OrchestrationResult>();

    // Computed properties

    /// <summary>
    /// Whether this orchestration has completed successfully
    /// </summary>
    [NotMapped]
    public bool IsCompleted => Status == "Completed" && CompletedAt.HasValue;

    /// <summary>
    /// Whether this orchestration has failed
    /// </summary>
    [NotMapped]
    public bool IsFailed => Status == "Failed" || Status == "Error";

    /// <summary>
    /// Whether this orchestration is currently running
    /// </summary>
    [NotMapped]
    public bool IsRunning => Status == "Running" || Status == "InProgress";

    /// <summary>
    /// Average tokens per agent (if applicable)
    /// </summary>
    [NotMapped]
    public double? AverageTokensPerAgent
    {
        get
        {
            if (TotalTokens == null || AgentCount == 0) return null;
            return (double)TotalTokens.Value / AgentCount;
        }
    }

    /// <summary>
    /// Average messages per agent (if applicable)
    /// </summary>
    [NotMapped]
    public double? AverageMessagesPerAgent
    {
        get
        {
            if (AgentCount == 0) return null;
            return (double)MessageCount / AgentCount;
        }
    }

    /// <summary>
    /// Tokens per second (if completed)
    /// </summary>
    [NotMapped]
    public double? TokensPerSecond
    {
        get
        {
            if (DurationSeconds == null || DurationSeconds == 0 || TotalTokens == null) return null;
            return (double)TotalTokens.Value / DurationSeconds.Value;
        }
    }

    /// <summary>
    /// Cost per token (if applicable)
    /// </summary>
    [NotMapped]
    public decimal? CostPerToken
    {
        get
        {
            if (TotalCost == null || TotalTokens == null || TotalTokens == 0) return null;
            return TotalCost.Value / TotalTokens.Value;
        }
    }

    /// <summary>
    /// Whether this orchestration is nested
    /// </summary>
    [NotMapped]
    public bool IsNested => ParentOrchestrationId.HasValue;

    /// <summary>
    /// Whether this orchestration has child orchestrations
    /// </summary>
    [NotMapped]
    public bool HasChildren => ChildOrchestrations.Count > 0;
}