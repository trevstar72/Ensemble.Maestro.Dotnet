namespace Ensemble.Maestro.Dotnet.Core.Services;

/// <summary>
/// Service interface for managing cross-database references between SQL, Neo4j, and Elasticsearch
/// </summary>
public interface ICrossReferenceService
{
    /// <summary>
    /// Create a coordinated cross-reference entry across all three databases
    /// </summary>
    Task<CrossReference> CreateCrossReferenceAsync(string entityType, object entityData, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get cross-reference by the primary ID
    /// </summary>
    Task<CrossReference?> GetCrossReferenceAsync(Guid primaryId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Update cross-reference with new database IDs
    /// </summary>
    Task<bool> UpdateCrossReferenceAsync(Guid primaryId, CrossReference updatedReference, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Delete cross-reference and associated records from all databases
    /// </summary>
    Task<bool> DeleteCrossReferenceAsync(Guid primaryId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Find orphaned records across databases
    /// </summary>
    Task<List<OrphanedRecord>> FindOrphanedRecordsAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Clean up orphaned records
    /// </summary>
    Task<int> CleanupOrphanedRecordsAsync(List<OrphanedRecord> orphans, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Validate cross-reference integrity across all databases
    /// </summary>
    Task<CrossReferenceValidationResult> ValidateIntegrityAsync(Guid primaryId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Cross-reference entry linking records across databases
/// </summary>
public class CrossReference
{
    /// <summary>
    /// Primary unique identifier used across all systems
    /// </summary>
    public Guid PrimaryId { get; set; }
    
    /// <summary>
    /// Type of entity (FunctionSpec, DesignerOutput, CodeUnit, etc.)
    /// </summary>
    public string EntityType { get; set; } = string.Empty;
    
    /// <summary>
    /// SQL Server record ID
    /// </summary>
    public Guid? SqlId { get; set; }
    
    /// <summary>
    /// Neo4j node ID or UUID
    /// </summary>
    public string? Neo4jId { get; set; }
    
    /// <summary>
    /// Elasticsearch document ID
    /// </summary>
    public string? ElasticsearchId { get; set; }
    
    /// <summary>
    /// Additional metadata for the reference
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
    
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
    public CrossReferenceStatus Status { get; set; } = CrossReferenceStatus.Active;
}

/// <summary>
/// Status of cross-reference entry
/// </summary>
public enum CrossReferenceStatus
{
    Active,
    PartiallyOrphaned,
    Orphaned,
    PendingDeletion
}

/// <summary>
/// Represents an orphaned record in one of the databases
/// </summary>
public class OrphanedRecord
{
    public string Database { get; set; } = string.Empty; // "SQL", "Neo4j", "Elasticsearch"
    public string EntityType { get; set; } = string.Empty;
    public string RecordId { get; set; } = string.Empty;
    public DateTime DiscoveredAt { get; set; } = DateTime.UtcNow;
    public string? Reason { get; set; }
}

/// <summary>
/// Result of cross-reference validation
/// </summary>
public class CrossReferenceValidationResult
{
    public bool IsValid { get; set; }
    public List<string> ValidationErrors { get; set; } = new();
    public bool HasOrphanedReferences { get; set; }
    public Dictionary<string, bool> DatabasePresence { get; set; } = new();
}