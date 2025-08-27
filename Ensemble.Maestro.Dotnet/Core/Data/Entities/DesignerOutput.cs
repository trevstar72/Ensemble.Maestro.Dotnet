using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ensemble.Maestro.Dotnet.Core.Data.Entities;

/// <summary>
/// Designer agent output containing system design specifications
/// </summary>
[Table("DesignerOutputs")]
public class DesignerOutput
{
    [Key]
    public Guid Id { get; set; }
    
    /// <summary>
    /// Reference to cross-database coordination record
    /// </summary>
    [ForeignKey(nameof(CrossReferenceEntity))]
    public Guid CrossReferenceId { get; set; }
    
    /// <summary>
    /// Project this output belongs to
    /// </summary>
    [ForeignKey(nameof(Project))]
    public Guid ProjectId { get; set; }
    
    /// <summary>
    /// Pipeline execution that created this output
    /// </summary>
    [ForeignKey(nameof(PipelineExecution))]
    public Guid PipelineExecutionId { get; set; }
    
    /// <summary>
    /// Agent execution that created this output
    /// </summary>
    [ForeignKey(nameof(AgentExecution))]
    public Guid AgentExecutionId { get; set; }
    
    /// <summary>
    /// Type of designer agent that created this
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string AgentType { get; set; } = string.Empty;
    
    /// <summary>
    /// Name of the designer agent
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string AgentName { get; set; } = string.Empty;
    
    /// <summary>
    /// Design output title
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string Title { get; set; } = string.Empty;
    
    /// <summary>
    /// Design output description
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// Target programming language
    /// </summary>
    [MaxLength(50)]
    public string? TargetLanguage { get; set; }
    
    /// <summary>
    /// Target deployment environment
    /// </summary>
    [MaxLength(100)]
    public string? DeploymentTarget { get; set; }
    
    /// <summary>
    /// Raw markdown output from the designer agent
    /// </summary>
    [Required]
    public string MarkdownOutput { get; set; } = string.Empty;
    
    /// <summary>
    /// Structured design data (JSON)
    /// </summary>
    public string? StructuredData { get; set; }
    
    /// <summary>
    /// System architecture overview
    /// </summary>
    public string? ArchitectureOverview { get; set; }
    
    /// <summary>
    /// Component specifications (JSON)
    /// </summary>
    public string? ComponentSpecs { get; set; }
    
    /// <summary>
    /// API design specifications (JSON)
    /// </summary>
    public string? ApiSpecs { get; set; }
    
    /// <summary>
    /// Database schema design (JSON)
    /// </summary>
    public string? DatabaseSpecs { get; set; }
    
    /// <summary>
    /// UI/UX design specifications (JSON)
    /// </summary>
    public string? UiSpecs { get; set; }
    
    /// <summary>
    /// Security design specifications
    /// </summary>
    public string? SecuritySpecs { get; set; }
    
    /// <summary>
    /// Performance and scalability requirements
    /// </summary>
    public string? PerformanceSpecs { get; set; }
    
    /// <summary>
    /// Integration specifications (JSON)
    /// </summary>
    public string? IntegrationSpecs { get; set; }
    
    /// <summary>
    /// Testing strategies and requirements
    /// </summary>
    public string? TestingSpecs { get; set; }
    
    /// <summary>
    /// Deployment and infrastructure specs
    /// </summary>
    public string? DeploymentSpecs { get; set; }
    
    /// <summary>
    /// Extracted function specifications count
    /// </summary>
    public int FunctionSpecsCount { get; set; } = 0;
    
    /// <summary>
    /// Estimated complexity for entire design (1-10)
    /// </summary>
    public int ComplexityRating { get; set; } = 5;
    
    /// <summary>
    /// Estimated total implementation hours
    /// </summary>
    public decimal? EstimatedHours { get; set; }
    
    /// <summary>
    /// Quality assessment score (1-100)
    /// </summary>
    public int QualityScore { get; set; }
    
    /// <summary>
    /// Confidence score from the designer agent (1-100)
    /// </summary>
    public int ConfidenceScore { get; set; }
    
    /// <summary>
    /// Current processing status
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = "Created";
    
    /// <summary>
    /// Processing stage (Created, Parsed, SpecsExtracted, Approved, etc.)
    /// </summary>
    [MaxLength(50)]
    public string? ProcessingStage { get; set; }
    
    /// <summary>
    /// Token usage for input
    /// </summary>
    public int? InputTokens { get; set; }
    
    /// <summary>
    /// Token usage for output
    /// </summary>
    public int? OutputTokens { get; set; }
    
    /// <summary>
    /// Execution cost
    /// </summary>
    [Column(TypeName = "decimal(10,6)")]
    public decimal? ExecutionCost { get; set; }
    
    /// <summary>
    /// Processing error message (if any)
    /// </summary>
    public string? ErrorMessage { get; set; }
    
    /// <summary>
    /// Additional metadata (JSON)
    /// </summary>
    public string? Metadata { get; set; }
    
    /// <summary>
    /// Tags for categorization (JSON array)
    /// </summary>
    public string? Tags { get; set; }
    
    /// <summary>
    /// Version number for this output
    /// </summary>
    public int Version { get; set; } = 1;
    
    /// <summary>
    /// Creation timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Last update timestamp
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// When this output was approved
    /// </summary>
    public DateTime? ApprovedAt { get; set; }
    
    /// <summary>
    /// When function specs extraction was completed
    /// </summary>
    public DateTime? SpecsExtractedAt { get; set; }
    
    // Navigation properties
    public virtual CrossReferenceEntity? CrossReference { get; set; }
    public virtual Project? Project { get; set; }
    public virtual PipelineExecution? PipelineExecution { get; set; }
    public virtual AgentExecution? AgentExecution { get; set; }
    
    /// <summary>
    /// Function specifications extracted from this designer output
    /// </summary>
    public virtual ICollection<FunctionSpecification> FunctionSpecifications { get; set; } = new List<FunctionSpecification>();
}