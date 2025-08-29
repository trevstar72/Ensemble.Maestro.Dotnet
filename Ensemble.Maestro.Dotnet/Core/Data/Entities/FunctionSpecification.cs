using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ensemble.Maestro.Dotnet.Core.Data.Entities;

/// <summary>
/// Function specification created by Designer agents
/// </summary>
[Table("FunctionSpecifications")]
public class FunctionSpecification
{
    [Key]
    public Guid Id { get; set; }
    
    /// <summary>
    /// Reference to cross-database coordination record
    /// </summary>
    [ForeignKey(nameof(CrossReferenceEntity))]
    public Guid CrossReferenceId { get; set; }
    
    /// <summary>
    /// Project this specification belongs to (nullable for MVP)
    /// </summary>
    public Guid? ProjectId { get; set; }
    
    /// <summary>
    /// Module this specification belongs to (if applicable)
    /// </summary>
    public Guid? ModuleId { get; set; }
    
    /// <summary>
    /// Pipeline execution that created this specification (nullable for MVP)
    /// </summary>
    public Guid? PipelineExecutionId { get; set; }
    
    /// <summary>
    /// Agent execution that created this specification (nullable for MVP)
    /// </summary>
    public Guid? AgentExecutionId { get; set; }
    
    /// <summary>
    /// Function name
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string FunctionName { get; set; } = string.Empty;
    
    /// <summary>
    /// Code unit (class, module, etc.) this function belongs to
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string CodeUnit { get; set; } = string.Empty;
    
    /// <summary>
    /// Namespace or package the function belongs to
    /// </summary>
    [MaxLength(500)]
    public string? Namespace { get; set; }
    
    /// <summary>
    /// Programming language
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Language { get; set; } = string.Empty;
    
    /// <summary>
    /// Function signature (method signature, prototype)
    /// </summary>
    [Required]
    public string Signature { get; set; } = string.Empty;
    
    /// <summary>
    /// Function description and purpose
    /// </summary>
    [Required]
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// Input parameters specification (JSON)
    /// </summary>
    public string? InputParameters { get; set; }
    
    /// <summary>
    /// Return type specification (JSON)
    /// </summary>
    public string? ReturnType { get; set; }
    
    /// <summary>
    /// Dependencies this function requires (JSON array)
    /// </summary>
    public string? Dependencies { get; set; }
    
    /// <summary>
    /// Business logic and implementation requirements
    /// </summary>
    public string? BusinessLogic { get; set; }
    
    /// <summary>
    /// Validation rules (JSON)
    /// </summary>
    public string? ValidationRules { get; set; }
    
    /// <summary>
    /// Error handling requirements
    /// </summary>
    public string? ErrorHandling { get; set; }
    
    /// <summary>
    /// Performance requirements
    /// </summary>
    public string? PerformanceRequirements { get; set; }
    
    /// <summary>
    /// Security considerations
    /// </summary>
    public string? SecurityConsiderations { get; set; }
    
    /// <summary>
    /// Test cases and scenarios (JSON)
    /// </summary>
    public string? TestCases { get; set; }
    
    /// <summary>
    /// Complexity rating (1-10)
    /// </summary>
    public int ComplexityRating { get; set; } = 5;
    
    /// <summary>
    /// Estimated implementation time in minutes
    /// </summary>
    public int? EstimatedMinutes { get; set; }
    
    /// <summary>
    /// Priority level
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string Priority { get; set; } = "Medium";
    
    /// <summary>
    /// Current status of the specification
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = "Draft";
    
    /// <summary>
    /// Designer agent that created this specification
    /// </summary>
    [MaxLength(100)]
    public string? CreatedByAgent { get; set; }
    
    /// <summary>
    /// Version number for this specification
    /// </summary>
    public int Version { get; set; } = 1;
    
    /// <summary>
    /// Additional metadata (JSON)
    /// </summary>
    public string? Metadata { get; set; }
    
    /// <summary>
    /// Tags for categorization (JSON array)
    /// </summary>
    public string? Tags { get; set; }
    
    /// <summary>
    /// Quality assessment score (1-100)
    /// </summary>
    public int? QualityScore { get; set; }
    
    /// <summary>
    /// Specification completeness percentage (0-100)
    /// </summary>
    public int CompletenessPercentage { get; set; } = 0;
    
    /// <summary>
    /// Creation timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Last update timestamp
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// When this specification was approved for implementation
    /// </summary>
    public DateTime? ApprovedAt { get; set; }
    
    /// <summary>
    /// When implementation was completed
    /// </summary>
    public DateTime? ImplementedAt { get; set; }
    
    // Navigation properties
    public virtual CrossReferenceEntity? CrossReference { get; set; }
    public virtual Project? Project { get; set; }
    public virtual Module? Module { get; set; }
    public virtual PipelineExecution? PipelineExecution { get; set; }
    public virtual AgentExecution? AgentExecution { get; set; }
}