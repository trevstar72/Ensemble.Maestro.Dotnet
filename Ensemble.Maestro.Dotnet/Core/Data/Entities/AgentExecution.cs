using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ensemble.Maestro.Dotnet.Core.Data.Entities;

/// <summary>
/// Represents the execution of a single Semantic Kernel agent
/// </summary>
public class AgentExecution
{
    /// <summary>
    /// Unique identifier for the agent execution
    /// </summary>
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// ID of the project this execution belongs to
    /// </summary>
    [Required]
    public Guid ProjectId { get; set; }

    /// <summary>
    /// ID of the pipeline execution (if part of a pipeline)
    /// </summary>
    public Guid? PipelineExecutionId { get; set; }

    /// <summary>
    /// ID of the stage execution (if part of a specific stage)
    /// </summary>
    public Guid? StageExecutionId { get; set; }

    /// <summary>
    /// Type/role of the agent (Planner, Designer, Builder, Validator, etc.)
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string AgentType { get; set; } = string.Empty;

    /// <summary>
    /// Specific agent name/identifier
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string AgentName { get; set; } = string.Empty;

    /// <summary>
    /// Agent specialization or sub-type (e.g., "CSharpBuilder", "ReactDesigner")
    /// </summary>
    [MaxLength(100)]
    public string? AgentSpecialization { get; set; }

    /// <summary>
    /// Current execution status
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = "Pending";

    /// <summary>
    /// Priority level of this execution
    /// </summary>
    [MaxLength(20)]
    public string Priority { get; set; } = "Medium";

    /// <summary>
    /// When the agent execution started
    /// </summary>
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the agent execution completed (if applicable)
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Duration of the execution in seconds
    /// </summary>
    public int? DurationSeconds { get; set; }

    /// <summary>
    /// Input prompt or data provided to the agent
    /// </summary>
    [Required]
    public string InputPrompt { get; set; } = string.Empty;

    /// <summary>
    /// Output response from the agent
    /// </summary>
    public string? OutputResponse { get; set; }

    /// <summary>
    /// Input token count
    /// </summary>
    public int? InputTokens { get; set; }

    /// <summary>
    /// Output token count
    /// </summary>
    public int? OutputTokens { get; set; }

    /// <summary>
    /// Total token count (input + output)
    /// </summary>
    public int? TotalTokens { get; set; }

    /// <summary>
    /// Cost of this execution in USD
    /// </summary>
    [Column(TypeName = "decimal(10,6)")]
    public decimal? ExecutionCost { get; set; }

    /// <summary>
    /// AI model used for this execution
    /// </summary>
    [MaxLength(100)]
    public string? ModelUsed { get; set; }

    /// <summary>
    /// Model temperature setting
    /// </summary>
    public float? Temperature { get; set; }

    /// <summary>
    /// Max tokens setting
    /// </summary>
    public int? MaxTokens { get; set; }

    /// <summary>
    /// Agent configuration as JSON
    /// </summary>
    public string? AgentConfig { get; set; }

    /// <summary>
    /// Function calls made by the agent as JSON
    /// </summary>
    public string? FunctionCalls { get; set; }

    /// <summary>
    /// Plugins used during this execution as JSON array
    /// </summary>
    public string? PluginsUsed { get; set; }

    /// <summary>
    /// Context data provided to the agent as JSON
    /// </summary>
    public string? ContextData { get; set; }

    /// <summary>
    /// Quality score of the output (0-100)
    /// </summary>
    public int? QualityScore { get; set; }

    /// <summary>
    /// Confidence score of the output (0-100)
    /// </summary>
    public int? ConfidenceScore { get; set; }

    /// <summary>
    /// Retry attempt number (0 for first attempt)
    /// </summary>
    public int RetryAttempt { get; set; } = 0;

    /// <summary>
    /// Maximum retry attempts allowed
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Error message if the execution failed
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
    /// Performance metrics as JSON
    /// </summary>
    public string? PerformanceMetrics { get; set; }

    /// <summary>
    /// Metadata about the execution as JSON
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// Tags for categorization
    /// </summary>
    public string? Tags { get; set; }

    /// <summary>
    /// Parent agent execution ID (for hierarchical executions)
    /// </summary>
    public Guid? ParentExecutionId { get; set; }

    /// <summary>
    /// Orchestration pattern used (GroupChat, Sequential, Handoff, etc.)
    /// </summary>
    [MaxLength(50)]
    public string? OrchestrationPattern { get; set; }

    // Navigation properties

    /// <summary>
    /// The project this execution belongs to
    /// </summary>
    [ForeignKey(nameof(ProjectId))]
    public virtual Project Project { get; set; } = null!;

    /// <summary>
    /// The pipeline execution this belongs to (if applicable)
    /// </summary>
    [ForeignKey(nameof(PipelineExecutionId))]
    public virtual PipelineExecution? PipelineExecution { get; set; }

    /// <summary>
    /// The stage execution this belongs to (if applicable)
    /// </summary>
    [ForeignKey(nameof(StageExecutionId))]
    public virtual StageExecution? StageExecution { get; set; }

    /// <summary>
    /// Parent agent execution (for hierarchical executions)
    /// </summary>
    [ForeignKey(nameof(ParentExecutionId))]
    public virtual AgentExecution? ParentExecution { get; set; }

    /// <summary>
    /// Child agent executions (for hierarchical executions)
    /// </summary>
    public virtual ICollection<AgentExecution> ChildExecutions { get; set; } = new List<AgentExecution>();

    /// <summary>
    /// Messages exchanged during this execution
    /// </summary>
    public virtual ICollection<AgentMessage> Messages { get; set; } = new List<AgentMessage>();

    // Computed properties

    /// <summary>
    /// Whether this execution has completed successfully
    /// </summary>
    [NotMapped]
    public bool IsCompleted => Status == "Completed" && CompletedAt.HasValue;

    /// <summary>
    /// Whether this execution has failed
    /// </summary>
    [NotMapped]
    public bool IsFailed => Status == "Failed" || Status == "Error";

    /// <summary>
    /// Whether this execution is currently running
    /// </summary>
    [NotMapped]
    public bool IsRunning => Status == "Running" || Status == "InProgress";

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
            if (ExecutionCost == null || TotalTokens == null || TotalTokens == 0) return null;
            return ExecutionCost.Value / TotalTokens.Value;
        }
    }
}