namespace Ensemble.Maestro.Dotnet.Core.Services;

/// <summary>
/// Service interface for Elasticsearch operations
/// </summary>
public interface IElasticsearchService
{
    /// <summary>
    /// Index a document in Elasticsearch
    /// </summary>
    Task<string> IndexDocumentAsync(string entityType, object document, Guid primaryId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Update an existing document in Elasticsearch
    /// </summary>
    Task<bool> UpdateDocumentAsync(string entityType, string documentId, object document, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Delete a document from Elasticsearch
    /// </summary>
    Task<bool> DeleteDocumentAsync(string entityType, string documentId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Check if a document exists in Elasticsearch
    /// </summary>
    Task<bool> DocumentExistsAsync(string entityType, string documentId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get a document from Elasticsearch
    /// </summary>
    Task<T?> GetDocumentAsync<T>(string entityType, string documentId, CancellationToken cancellationToken = default) where T : class;
    
    /// <summary>
    /// Search documents in Elasticsearch
    /// </summary>
    Task<List<T>> SearchDocumentsAsync<T>(string entityType, string query, int size = 100, CancellationToken cancellationToken = default) where T : class;
    
    /// <summary>
    /// Search documents with advanced filters
    /// </summary>
    Task<ElasticsearchSearchResult<T>> SearchWithFiltersAsync<T>(
        string entityType, 
        string? query = null, 
        Dictionary<string, object>? filters = null,
        int from = 0,
        int size = 100,
        CancellationToken cancellationToken = default) where T : class;
    
    /// <summary>
    /// Create or update index mapping
    /// </summary>
    Task<bool> CreateIndexMappingAsync(string entityType, object mapping, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Bulk index multiple documents
    /// </summary>
    Task<BulkIndexResult> BulkIndexAsync(string entityType, IEnumerable<object> documents, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Store designer output with function specifications and code units in Elasticsearch for full-text search
    /// </summary>
    Task<bool> StoreDesignerOutputAsync(
        Core.Data.Entities.DesignerOutput designerOutput,
        List<Core.Data.Entities.FunctionSpecification> functionSpecs,
        List<Core.Data.Entities.CodeUnit> codeUnits,
        Guid crossRefId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of Elasticsearch search operation
/// </summary>
public class ElasticsearchSearchResult<T>
{
    public List<T> Documents { get; set; } = new();
    public long TotalHits { get; set; }
    public int From { get; set; }
    public int Size { get; set; }
    public bool HasMore => From + Size < TotalHits;
}

/// <summary>
/// Result of bulk index operation
/// </summary>
public class BulkIndexResult
{
    public int SuccessCount { get; set; }
    public int ErrorCount { get; set; }
    public List<string> Errors { get; set; } = new();
    public bool HasErrors => ErrorCount > 0;
}