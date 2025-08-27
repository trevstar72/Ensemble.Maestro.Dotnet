using Ensemble.Maestro.Dotnet.Core.Data;
using Ensemble.Maestro.Dotnet.Core.Data.Entities;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Security.Cryptography;
using System.Text;

namespace Ensemble.Maestro.Dotnet.Core.Services;

/// <summary>
/// Implementation of cross-database reference management
/// </summary>
public class CrossReferenceService : ICrossReferenceService
{
    private readonly MaestroDbContext _dbContext;
    private readonly INeo4jService _neo4jService;
    private readonly IElasticsearchService _elasticsearchService;
    private readonly ILogger<CrossReferenceService> _logger;

    public CrossReferenceService(
        MaestroDbContext dbContext,
        INeo4jService neo4jService,
        IElasticsearchService elasticsearchService,
        ILogger<CrossReferenceService> logger)
    {
        _dbContext = dbContext;
        _neo4jService = neo4jService;
        _elasticsearchService = elasticsearchService;
        _logger = logger;
    }

    public async Task<CrossReference> CreateCrossReferenceAsync(string entityType, object entityData, CancellationToken cancellationToken = default)
    {
        var primaryId = Guid.NewGuid();
        
        _logger.LogInformation("Creating cross-reference for {EntityType} with ID {PrimaryId}", entityType, primaryId);

        var crossRef = new CrossReference
        {
            PrimaryId = primaryId,
            EntityType = entityType,
            Metadata = ExtractMetadata(entityData)
        };

        try
        {
            // Store in SQL Server first (as the source of truth)
            var entity = new CrossReferenceEntity
            {
                PrimaryId = crossRef.PrimaryId,
                EntityType = crossRef.EntityType,
                Metadata = JsonSerializer.Serialize(crossRef.Metadata),
                Status = CrossReferenceStatus.Active.ToString(),
                IntegrityHash = CalculateIntegrityHash(crossRef)
            };
            
            _dbContext.CrossReferences.Add(entity);
            await _dbContext.SaveChangesAsync(cancellationToken);
            crossRef.SqlId = entity.PrimaryId;

            // Store structured data in Neo4j
            if (_neo4jService != null)
            {
                var neo4jId = await _neo4jService.CreateNodeAsync(entityType, entityData, primaryId, cancellationToken);
                crossRef.Neo4jId = neo4jId;
                
                // Update SQL record with Neo4j ID
                entity.Neo4jId = neo4jId;
            }

            // Store searchable content in Elasticsearch
            if (_elasticsearchService != null)
            {
                var elasticsearchId = await _elasticsearchService.IndexDocumentAsync(entityType, entityData, primaryId, cancellationToken);
                crossRef.ElasticsearchId = elasticsearchId;
                
                // Update SQL record with Elasticsearch ID
                entity.ElasticsearchId = elasticsearchId;
            }

            // Update integrity hash with all IDs
            entity.IntegrityHash = CalculateIntegrityHash(crossRef);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Cross-reference created successfully: SQL={SqlId}, Neo4j={Neo4jId}, ES={ElasticsearchId}", 
                crossRef.SqlId, crossRef.Neo4jId, crossRef.ElasticsearchId);

            return crossRef;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create cross-reference for {EntityType} with ID {PrimaryId}", entityType, primaryId);
            
            // Attempt cleanup of partially created records
            await CleanupPartialRecordAsync(crossRef);
            throw;
        }
    }

    public async Task<CrossReference?> GetCrossReferenceAsync(Guid primaryId, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.CrossReferences
            .FirstOrDefaultAsync(cr => cr.PrimaryId == primaryId, cancellationToken);

        if (entity == null)
            return null;

        return MapToDto(entity);
    }

    public async Task<bool> UpdateCrossReferenceAsync(Guid primaryId, CrossReference updatedReference, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.CrossReferences
            .FirstOrDefaultAsync(cr => cr.PrimaryId == primaryId, cancellationToken);

        if (entity == null)
            return false;

        entity.Neo4jId = updatedReference.Neo4jId;
        entity.ElasticsearchId = updatedReference.ElasticsearchId;
        entity.Metadata = JsonSerializer.Serialize(updatedReference.Metadata);
        entity.Status = updatedReference.Status.ToString();
        entity.IntegrityHash = CalculateIntegrityHash(updatedReference);

        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteCrossReferenceAsync(Guid primaryId, CancellationToken cancellationToken = default)
    {
        var crossRef = await GetCrossReferenceAsync(primaryId, cancellationToken);
        if (crossRef == null)
            return false;

        try
        {
            // Delete from all databases
            var tasks = new List<Task>();

            if (!string.IsNullOrEmpty(crossRef.Neo4jId) && _neo4jService != null)
                tasks.Add(_neo4jService.DeleteNodeAsync(crossRef.Neo4jId, cancellationToken));

            if (!string.IsNullOrEmpty(crossRef.ElasticsearchId) && _elasticsearchService != null)
                tasks.Add(_elasticsearchService.DeleteDocumentAsync(crossRef.EntityType, crossRef.ElasticsearchId, cancellationToken));

            await Task.WhenAll(tasks);

            // Delete from SQL (source of truth) last
            await _dbContext.CrossReferences
                .Where(cr => cr.PrimaryId == primaryId)
                .ExecuteDeleteAsync(cancellationToken);

            _logger.LogInformation("Cross-reference deleted successfully: {PrimaryId}", primaryId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete cross-reference: {PrimaryId}", primaryId);
            return false;
        }
    }

    public async Task<List<OrphanedRecord>> FindOrphanedRecordsAsync(CancellationToken cancellationToken = default)
    {
        var orphans = new List<OrphanedRecord>();

        // Find records in SQL that don't have corresponding Neo4j or Elasticsearch entries
        var crossRefs = await _dbContext.CrossReferences
            .Where(cr => cr.Status != CrossReferenceStatus.Orphaned.ToString())
            .ToListAsync(cancellationToken);

        foreach (var crossRef in crossRefs)
        {
            var dto = MapToDto(crossRef);
            var validation = await ValidateIntegrityAsync(dto.PrimaryId, cancellationToken);
            
            if (validation.HasOrphanedReferences)
            {
                foreach (var error in validation.ValidationErrors)
                {
                    orphans.Add(new OrphanedRecord
                    {
                        Database = ExtractDatabaseFromError(error),
                        EntityType = dto.EntityType,
                        RecordId = dto.PrimaryId.ToString(),
                        Reason = error
                    });
                }
            }
        }

        return orphans;
    }

    public async Task<int> CleanupOrphanedRecordsAsync(List<OrphanedRecord> orphans, CancellationToken cancellationToken = default)
    {
        var cleanedCount = 0;

        foreach (var orphan in orphans)
        {
            try
            {
                if (Guid.TryParse(orphan.RecordId, out var primaryId))
                {
                    var deleted = await DeleteCrossReferenceAsync(primaryId, cancellationToken);
                    if (deleted)
                    {
                        cleanedCount++;
                        _logger.LogInformation("Cleaned orphaned record: {Database} {EntityType} {RecordId}", 
                            orphan.Database, orphan.EntityType, orphan.RecordId);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to clean orphaned record: {Database} {EntityType} {RecordId}", 
                    orphan.Database, orphan.EntityType, orphan.RecordId);
            }
        }

        return cleanedCount;
    }

    public async Task<CrossReferenceValidationResult> ValidateIntegrityAsync(Guid primaryId, CancellationToken cancellationToken = default)
    {
        var result = new CrossReferenceValidationResult();
        var crossRef = await GetCrossReferenceAsync(primaryId, cancellationToken);

        if (crossRef == null)
        {
            result.ValidationErrors.Add("Cross-reference not found in SQL Server");
            return result;
        }

        // Check presence in each database
        result.DatabasePresence["SQL"] = true; // We found it above

        // Check Neo4j
        if (!string.IsNullOrEmpty(crossRef.Neo4jId) && _neo4jService != null)
        {
            result.DatabasePresence["Neo4j"] = await _neo4jService.NodeExistsAsync(crossRef.Neo4jId, cancellationToken);
            if (!result.DatabasePresence["Neo4j"])
            {
                result.ValidationErrors.Add($"Neo4j node {crossRef.Neo4jId} not found");
                result.HasOrphanedReferences = true;
            }
        }

        // Check Elasticsearch
        if (!string.IsNullOrEmpty(crossRef.ElasticsearchId) && _elasticsearchService != null)
        {
            result.DatabasePresence["Elasticsearch"] = await _elasticsearchService.DocumentExistsAsync(crossRef.EntityType, crossRef.ElasticsearchId, cancellationToken);
            if (!result.DatabasePresence["Elasticsearch"])
            {
                result.ValidationErrors.Add($"Elasticsearch document {crossRef.ElasticsearchId} not found");
                result.HasOrphanedReferences = true;
            }
        }

        result.IsValid = result.ValidationErrors.Count == 0;
        return result;
    }

    private async Task CleanupPartialRecordAsync(CrossReference crossRef)
    {
        try
        {
            if (crossRef.SqlId.HasValue)
            {
                await _dbContext.CrossReferences
                    .Where(cr => cr.PrimaryId == crossRef.PrimaryId)
                    .ExecuteDeleteAsync();
            }

            if (!string.IsNullOrEmpty(crossRef.Neo4jId) && _neo4jService != null)
                await _neo4jService.DeleteNodeAsync(crossRef.Neo4jId);

            if (!string.IsNullOrEmpty(crossRef.ElasticsearchId) && _elasticsearchService != null)
                await _elasticsearchService.DeleteDocumentAsync(crossRef.EntityType, crossRef.ElasticsearchId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup partial record for {PrimaryId}", crossRef.PrimaryId);
        }
    }

    private static Dictionary<string, object> ExtractMetadata(object entityData)
    {
        // Extract relevant metadata from the entity data
        var metadata = new Dictionary<string, object>();
        
        if (entityData != null)
        {
            var type = entityData.GetType();
            metadata["DataType"] = type.Name;
            metadata["Assembly"] = type.Assembly.GetName().Name ?? "Unknown";
        }
        
        return metadata;
    }

    private static string CalculateIntegrityHash(CrossReference crossRef)
    {
        var hashData = $"{crossRef.PrimaryId}|{crossRef.EntityType}|{crossRef.SqlId}|{crossRef.Neo4jId}|{crossRef.ElasticsearchId}";
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(hashData));
        return Convert.ToHexString(hashBytes);
    }

    private static CrossReference MapToDto(CrossReferenceEntity entity)
    {
        var metadata = new Dictionary<string, object>();
        if (!string.IsNullOrEmpty(entity.Metadata))
        {
            try
            {
                metadata = JsonSerializer.Deserialize<Dictionary<string, object>>(entity.Metadata) ?? new Dictionary<string, object>();
            }
            catch
            {
                // If deserialization fails, use empty metadata
            }
        }

        return new CrossReference
        {
            PrimaryId = entity.PrimaryId,
            EntityType = entity.EntityType,
            SqlId = entity.SqlId,
            Neo4jId = entity.Neo4jId,
            ElasticsearchId = entity.ElasticsearchId,
            Metadata = metadata,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
            Status = Enum.TryParse<CrossReferenceStatus>(entity.Status, out var status) ? status : CrossReferenceStatus.Active
        };
    }

    private static string ExtractDatabaseFromError(string error)
    {
        if (error.Contains("Neo4j", StringComparison.OrdinalIgnoreCase))
            return "Neo4j";
        if (error.Contains("Elasticsearch", StringComparison.OrdinalIgnoreCase))
            return "Elasticsearch";
        return "SQL";
    }
}