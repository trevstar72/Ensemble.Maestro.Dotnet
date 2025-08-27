using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ensemble.Maestro.Dotnet.Core.Data.Entities;

/// <summary>
/// Database entity for storing cross-database references
/// </summary>
[Table("CrossReferences")]
public class CrossReferenceEntity
{
    [Key]
    public Guid PrimaryId { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string EntityType { get; set; } = string.Empty;
    
    /// <summary>
    /// SQL Server record ID (when applicable)
    /// </summary>
    public Guid? SqlId { get; set; }
    
    /// <summary>
    /// Neo4j node ID or UUID
    /// </summary>
    [MaxLength(255)]
    public string? Neo4jId { get; set; }
    
    /// <summary>
    /// Elasticsearch document ID
    /// </summary>
    [MaxLength(255)]
    public string? ElasticsearchId { get; set; }
    
    /// <summary>
    /// Additional metadata stored as JSON
    /// </summary>
    public string? Metadata { get; set; }
    
    /// <summary>
    /// Creation timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Last update timestamp
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Status of the cross-reference
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = "Active";
    
    /// <summary>
    /// Hash of the cross-reference for integrity checking
    /// </summary>
    [MaxLength(64)]
    public string? IntegrityHash { get; set; }
    
    /// <summary>
    /// Last integrity check timestamp
    /// </summary>
    public DateTime? LastIntegrityCheck { get; set; }
}