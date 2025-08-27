using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ensemble.Maestro.Dotnet.Core.Data.Entities;

/// <summary>
/// Represents a module within a project - a logical grouping of related functionality
/// </summary>
public class Module
{
    /// <summary>
    /// Unique identifier for the module
    /// </summary>
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// ID of the project this module belongs to
    /// </summary>
    [Required]
    public Guid ProjectId { get; set; }

    /// <summary>
    /// Module name
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Module description
    /// </summary>
    [Required]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Module type/category (UI, API, Service, Data, etc.)
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string ModuleType { get; set; } = string.Empty;

    /// <summary>
    /// Technical stack/technologies used in this module
    /// </summary>
    public string? TechnicalStack { get; set; }

    /// <summary>
    /// Module complexity score (1-10)
    /// </summary>
    public int ComplexityScore { get; set; } = 1;

    /// <summary>
    /// Module priority level
    /// </summary>
    [MaxLength(20)]
    public string Priority { get; set; } = "Medium";

    /// <summary>
    /// Current status of the module
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = "Pending";

    /// <summary>
    /// Module specifications as JSON
    /// </summary>
    public string? Specifications { get; set; }

    /// <summary>
    /// Module dependencies as JSON array
    /// </summary>
    public string? Dependencies { get; set; }

    /// <summary>
    /// Estimated development hours
    /// </summary>
    public decimal? EstimatedHours { get; set; }

    /// <summary>
    /// Actual development hours spent
    /// </summary>
    public decimal? ActualHours { get; set; }

    /// <summary>
    /// Progress percentage (0-100)
    /// </summary>
    public int ProgressPercentage { get; set; } = 0;

    /// <summary>
    /// Module order within the project
    /// </summary>
    public int ModuleOrder { get; set; } = 0;

    /// <summary>
    /// Whether this module is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Module metadata as JSON
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// Module tags for categorization
    /// </summary>
    public string? Tags { get; set; }

    /// <summary>
    /// When the module was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the module was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the module was completed (if applicable)
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    // Navigation properties

    /// <summary>
    /// The project this module belongs to
    /// </summary>
    [ForeignKey(nameof(ProjectId))]
    public virtual Project Project { get; set; } = null!;

    /// <summary>
    /// Files that belong to this module
    /// </summary>
    public virtual ICollection<ProjectFile> Files { get; set; } = new List<ProjectFile>();

    // Computed properties

    /// <summary>
    /// Whether this module is completed
    /// </summary>
    [NotMapped]
    public bool IsCompleted => Status == "Completed" && CompletedAt.HasValue;

    /// <summary>
    /// Whether this module has failed
    /// </summary>
    [NotMapped]
    public bool IsFailed => Status == "Failed" || Status == "Error";

    /// <summary>
    /// Whether this module is currently being developed
    /// </summary>
    [NotMapped]
    public bool IsInProgress => Status == "InProgress" || Status == "Development";

    /// <summary>
    /// Efficiency ratio (estimated vs actual hours)
    /// </summary>
    [NotMapped]
    public double? EfficiencyRatio
    {
        get
        {
            if (EstimatedHours == null || ActualHours == null || ActualHours == 0) return null;
            return (double)(EstimatedHours.Value / ActualHours.Value);
        }
    }

    /// <summary>
    /// Number of files in this module
    /// </summary>
    [NotMapped]
    public int FileCount => Files?.Count ?? 0;
}