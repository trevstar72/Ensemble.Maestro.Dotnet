using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ensemble.Maestro.Dotnet.Core.Data.Entities;

/// <summary>
/// Code unit (class, module, etc.) managed by Code Unit Controllers
/// </summary>
[Table("CodeUnits")]
public class CodeUnit
{
    [Key]
    public Guid Id { get; set; }
    
    /// <summary>
    /// Reference to cross-database coordination record
    /// </summary>
    [ForeignKey(nameof(CrossReferenceEntity))]
    public Guid CrossReferenceId { get; set; }
    
    /// <summary>
    /// Project this code unit belongs to
    /// </summary>
    [ForeignKey(nameof(Project))]
    public Guid ProjectId { get; set; }
    
    /// <summary>
    /// Module this code unit belongs to (if applicable)
    /// </summary>
    [ForeignKey(nameof(Module))]
    public Guid? ModuleId { get; set; }
    
    /// <summary>
    /// Pipeline execution that created this code unit
    /// </summary>
    [ForeignKey(nameof(PipelineExecution))]
    public Guid PipelineExecutionId { get; set; }
    
    /// <summary>
    /// Designer output this was extracted from
    /// </summary>
    [ForeignKey(nameof(DesignerOutput))]
    public Guid? DesignerOutputId { get; set; }
    
    /// <summary>
    /// Name of the code unit (class name, module name)
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Type of code unit (class, interface, module, service, etc.)
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string UnitType { get; set; } = string.Empty;
    
    /// <summary>
    /// Namespace or package
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
    /// Full file path where this code unit will be generated
    /// </summary>
    [MaxLength(1000)]
    public string? FilePath { get; set; }
    
    /// <summary>
    /// Description and purpose of this code unit
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// Base classes or interfaces this unit inherits from (JSON array)
    /// </summary>
    public string? Inheritance { get; set; }
    
    /// <summary>
    /// Dependencies this code unit requires (JSON array)
    /// </summary>
    public string? Dependencies { get; set; }
    
    /// <summary>
    /// Fields, properties, constants (JSON array)
    /// </summary>
    public string? Fields { get; set; }
    
    /// <summary>
    /// Constructor specifications (JSON array)
    /// </summary>
    public string? Constructors { get; set; }
    
    /// <summary>
    /// Design patterns used (JSON array)
    /// </summary>
    public string? Patterns { get; set; }
    
    /// <summary>
    /// Responsibility and business logic description
    /// </summary>
    public string? Responsibilities { get; set; }
    
    /// <summary>
    /// Integration points and interfaces (JSON)
    /// </summary>
    public string? Integrations { get; set; }
    
    /// <summary>
    /// Security considerations specific to this unit
    /// </summary>
    public string? SecurityConsiderations { get; set; }
    
    /// <summary>
    /// Performance requirements and optimizations
    /// </summary>
    public string? PerformanceConsiderations { get; set; }
    
    /// <summary>
    /// Testing strategy for this code unit
    /// </summary>
    public string? TestingStrategy { get; set; }
    
    /// <summary>
    /// Overall complexity rating (1-10)
    /// </summary>
    public int ComplexityRating { get; set; } = 5;
    
    /// <summary>
    /// Estimated implementation time in minutes
    /// </summary>
    public int? EstimatedMinutes { get; set; }
    
    /// <summary>
    /// Priority level for implementation
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string Priority { get; set; } = "Medium";
    
    /// <summary>
    /// Current status of the code unit
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = "Planned";
    
    /// <summary>
    /// Processing stage (Planned, ControllerAssigned, InProgress, Completed, etc.)
    /// </summary>
    [MaxLength(50)]
    public string? ProcessingStage { get; set; }
    
    /// <summary>
    /// Code Unit Controller agent assigned to this unit
    /// </summary>
    [MaxLength(100)]
    public string? AssignedController { get; set; }
    
    /// <summary>
    /// ID of the Code Unit Controller agent execution
    /// </summary>
    public Guid? ControllerExecutionId { get; set; }
    
    /// <summary>
    /// Number of functions in this code unit
    /// </summary>
    public int FunctionCount { get; set; } = 0;
    
    /// <summary>
    /// Number of simple functions (complexity < 4)
    /// </summary>
    public int SimpleFunctionCount { get; set; } = 0;
    
    /// <summary>
    /// Number of complex functions requiring Method Agents
    /// </summary>
    public int ComplexFunctionCount { get; set; } = 0;
    
    /// <summary>
    /// Number of Method Agents spawned for this unit
    /// </summary>
    public int MethodAgentCount { get; set; } = 0;
    
    /// <summary>
    /// Completion percentage (0-100)
    /// </summary>
    public int CompletionPercentage { get; set; } = 0;
    
    /// <summary>
    /// Quality assessment score (1-100)
    /// </summary>
    public int? QualityScore { get; set; }
    
    /// <summary>
    /// Generated source code (when implementation is complete)
    /// </summary>
    public string? GeneratedCode { get; set; }
    
    /// <summary>
    /// Generated code size in bytes
    /// </summary>
    public long? CodeSize { get; set; }
    
    /// <summary>
    /// Code generation statistics (JSON)
    /// </summary>
    public string? CodeStats { get; set; }
    
    /// <summary>
    /// Implementation errors (if any)
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
    /// Version number
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
    /// When controller was assigned
    /// </summary>
    public DateTime? ControllerAssignedAt { get; set; }
    
    /// <summary>
    /// When implementation started
    /// </summary>
    public DateTime? ImplementationStartedAt { get; set; }
    
    /// <summary>
    /// When implementation was completed
    /// </summary>
    public DateTime? ImplementationCompletedAt { get; set; }
    
    // Navigation properties
    public virtual CrossReferenceEntity? CrossReference { get; set; }
    public virtual Project? Project { get; set; }
    public virtual Module? Module { get; set; }
    public virtual PipelineExecution? PipelineExecution { get; set; }
    public virtual DesignerOutput? DesignerOutput { get; set; }
    
    /// <summary>
    /// Function specifications belonging to this code unit
    /// </summary>
    public virtual ICollection<FunctionSpecification> FunctionSpecifications { get; set; } = new List<FunctionSpecification>();
}