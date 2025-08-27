namespace Ensemble.Maestro.Dotnet.Core.Services;

/// <summary>
/// Service interface for Neo4j graph database operations
/// </summary>
public interface INeo4jService
{
    /// <summary>
    /// Create a new node in Neo4j with the given entity data
    /// </summary>
    Task<string> CreateNodeAsync(string entityType, object entityData, Guid primaryId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Update an existing node in Neo4j
    /// </summary>
    Task<bool> UpdateNodeAsync(string nodeId, object entityData, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Delete a node from Neo4j
    /// </summary>
    Task<bool> DeleteNodeAsync(string nodeId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Check if a node exists in Neo4j
    /// </summary>
    Task<bool> NodeExistsAsync(string nodeId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get node data from Neo4j
    /// </summary>
    Task<T?> GetNodeAsync<T>(string nodeId, CancellationToken cancellationToken = default) where T : class;
    
    /// <summary>
    /// Create a relationship between two nodes
    /// </summary>
    Task<bool> CreateRelationshipAsync(string fromNodeId, string toNodeId, string relationshipType, Dictionary<string, object>? properties = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Query nodes by type and properties
    /// </summary>
    Task<List<T>> QueryNodesAsync<T>(string entityType, Dictionary<string, object>? filters = null, CancellationToken cancellationToken = default) where T : class;
    
    /// <summary>
    /// Get related nodes through relationships
    /// </summary>
    Task<List<T>> GetRelatedNodesAsync<T>(string nodeId, string relationshipType, string direction = "out", CancellationToken cancellationToken = default) where T : class;
    
    /// <summary>
    /// Store designer output with function specifications and code units in Neo4j graph
    /// </summary>
    Task<bool> StoreDesignerOutputAsync(
        Core.Data.Entities.DesignerOutput designerOutput,
        List<Core.Data.Entities.FunctionSpecification> functionSpecs,
        List<Core.Data.Entities.CodeUnit> codeUnits,
        Guid crossRefId,
        CancellationToken cancellationToken = default);
}