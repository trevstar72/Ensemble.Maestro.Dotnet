using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ensemble.Maestro.Dotnet.Core.Data.Entities;

/// <summary>
/// Represents a project in the Maestro system - the main container for all generated code and artifacts
/// </summary>
public class Project
{
    /// <summary>
    /// Unique identifier for the project
    /// </summary>
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Human-readable project name
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Project description/requirements
    /// </summary>
    [Required]
    public string Requirements { get; set; } = string.Empty;

    /// <summary>
    /// Generated project charter (detailed specification)
    /// </summary>
    public string? Charter { get; set; }

    /// <summary>
    /// Current project status
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = "Pending";

    /// <summary>
    /// Target programming language(s) for code generation
    /// </summary>
    [MaxLength(100)]
    public string? TargetLanguage { get; set; }

    /// <summary>
    /// Target framework (e.g., Blazor, React, .NET Core)
    /// </summary>
    [MaxLength(100)]
    public string? TargetFramework { get; set; }

    /// <summary>
    /// Deployment target (e.g., Azure, AWS, On-Premise)
    /// </summary>
    [MaxLength(100)]
    public string? DeploymentTarget { get; set; }

    /// <summary>
    /// Project complexity score (1-10)
    /// </summary>
    public int? ComplexityScore { get; set; }

    /// <summary>
    /// Estimated development hours
    /// </summary>
    public decimal? EstimatedHours { get; set; }

    /// <summary>
    /// Project priority level
    /// </summary>
    [MaxLength(20)]
    public string Priority { get; set; } = "Medium";

    /// <summary>
    /// Project tags for categorization
    /// </summary>
    public string? Tags { get; set; }

    /// <summary>
    /// Additional metadata as JSON
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// When the project was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the project was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the project was completed (if applicable)
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    // Navigation properties
    
    /// <summary>
    /// All pipeline executions for this project
    /// </summary>
    public virtual ICollection<PipelineExecution> PipelineExecutions { get; set; } = new List<PipelineExecution>();

    /// <summary>
    /// All generated files for this project
    /// </summary>
    public virtual ICollection<ProjectFile> Files { get; set; } = new List<ProjectFile>();

    /// <summary>
    /// All agent executions for this project
    /// </summary>
    public virtual ICollection<AgentExecution> AgentExecutions { get; set; } = new List<AgentExecution>();

    /// <summary>
    /// All modules defined for this project
    /// </summary>
    public virtual ICollection<Module> Modules { get; set; } = new List<Module>();
}