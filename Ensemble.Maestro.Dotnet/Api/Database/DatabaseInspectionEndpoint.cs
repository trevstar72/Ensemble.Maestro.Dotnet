using FastEndpoints;
using Ensemble.Maestro.Dotnet.Core.Data;
using Microsoft.EntityFrameworkCore;

namespace Ensemble.Maestro.Dotnet.Api.Database;

public class DatabaseInspectionRequest
{
    public string? Action { get; set; } = "list"; // "list", "clear"
    public string? Table { get; set; } = "all"; // "codeunits", "functionspecs", "all"
}

public class DatabaseInspectionResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<CodeUnitInfo> CodeUnits { get; set; } = new();
    public List<FunctionSpecInfo> FunctionSpecs { get; set; } = new();
    public DatabaseStats Stats { get; set; } = new();
}

public class CodeUnitInfo
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? Namespace { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public int FunctionCount { get; set; }
}

public class FunctionSpecInfo
{
    public string Id { get; set; } = string.Empty;
    public string FunctionName { get; set; } = string.Empty;
    public string CodeUnitName { get; set; } = string.Empty;
    public string? Signature { get; set; }
    public string? Description { get; set; }
    public int ComplexityRating { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class DatabaseStats
{
    public int CodeUnitsCount { get; set; }
    public int FunctionSpecsCount { get; set; }
    public DateTime? LastCodeUnitCreated { get; set; }
    public DateTime? LastFunctionSpecCreated { get; set; }
}

/// <summary>
/// Database inspection endpoint for checking design specs and code units
/// </summary>
public class DatabaseInspectionEndpoint : Endpoint<DatabaseInspectionRequest, DatabaseInspectionResponse>
{
    private readonly MaestroDbContext _dbContext;
    private readonly ILogger<DatabaseInspectionEndpoint> _logger;

    public DatabaseInspectionEndpoint(MaestroDbContext dbContext, ILogger<DatabaseInspectionEndpoint> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public override void Configure()
    {
        Post("/api/database/inspect");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Inspect Database Content";
            s.Description = "List or clear design specs and code units from the database";
            s.ExampleRequest = new DatabaseInspectionRequest 
            { 
                Action = "list", 
                Table = "all" 
            };
        });
    }

    public override async Task HandleAsync(DatabaseInspectionRequest req, CancellationToken ct)
    {
        try
        {
            var response = new DatabaseInspectionResponse { Success = true };

            if (req.Action?.ToLower() == "clear")
            {
                await HandleClearAction(req.Table, response, ct);
            }
            else
            {
                await HandleListAction(req.Table, response, ct);
            }

            await Send.OkAsync(response, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in database inspection endpoint");
            
            await Send.ResponseAsync(new DatabaseInspectionResponse
            {
                Success = false,
                Message = $"Database inspection failed: {ex.Message}"
            }, 500, ct);
        }
    }

    private async Task HandleListAction(string? table, DatabaseInspectionResponse response, CancellationToken ct)
    {
        _logger.LogInformation("🔍 Listing database content for table: {Table}", table ?? "all");

        // Get stats
        response.Stats.CodeUnitsCount = await _dbContext.CodeUnits.CountAsync(ct);
        response.Stats.FunctionSpecsCount = await _dbContext.FunctionSpecifications.CountAsync(ct);
        
        if (response.Stats.CodeUnitsCount > 0)
        {
            response.Stats.LastCodeUnitCreated = await _dbContext.CodeUnits
                .MaxAsync(cu => (DateTime?)cu.CreatedAt, ct);
        }
        
        if (response.Stats.FunctionSpecsCount > 0)
        {
            response.Stats.LastFunctionSpecCreated = await _dbContext.FunctionSpecifications
                .MaxAsync(fs => (DateTime?)fs.CreatedAt, ct);
        }

        // Get detailed data if requested
        if (table?.ToLower() != "stats")
        {
            if (table?.ToLower() == "all" || table?.ToLower() == "codeunits")
            {
                response.CodeUnits = await _dbContext.CodeUnits
                    .Select(cu => new CodeUnitInfo
                    {
                        Id = cu.Id.ToString(),
                        Name = cu.Name,
                        Type = cu.UnitType,
                        Namespace = cu.Namespace,
                        Description = cu.Description,
                        CreatedAt = cu.CreatedAt,
                        FunctionCount = cu.FunctionSpecifications.Count
                    })
                    .OrderByDescending(cu => cu.CreatedAt)
                    .Take(50) // Limit to prevent large responses
                    .ToListAsync(ct);
            }

            if (table?.ToLower() == "all" || table?.ToLower() == "functionspecs")
            {
                response.FunctionSpecs = await _dbContext.FunctionSpecifications
                    .Select(fs => new FunctionSpecInfo
                    {
                        Id = fs.Id.ToString(),
                        FunctionName = fs.FunctionName,
                        CodeUnitName = fs.CodeUnit,
                        Signature = fs.Signature,
                        Description = fs.Description,
                        ComplexityRating = fs.ComplexityRating,
                        CreatedAt = fs.CreatedAt
                    })
                    .OrderByDescending(fs => fs.CreatedAt)
                    .Take(100) // Limit to prevent large responses
                    .ToListAsync(ct);
            }
        }

        response.Message = $"Database content retrieved. CodeUnits: {response.Stats.CodeUnitsCount}, FunctionSpecs: {response.Stats.FunctionSpecsCount}";
        
        _logger.LogInformation("📊 Database stats - CodeUnits: {CodeUnits}, FunctionSpecs: {FunctionSpecs}", 
            response.Stats.CodeUnitsCount, response.Stats.FunctionSpecsCount);
    }

    private async Task HandleClearAction(string? table, DatabaseInspectionResponse response, CancellationToken ct)
    {
        _logger.LogWarning("🗑️ Clearing database content for table: {Table}", table ?? "all");

        int clearedCodeUnits = 0;
        int clearedFunctionSpecs = 0;

        if (table?.ToLower() == "all" || table?.ToLower() == "functionspecs")
        {
            var functionSpecs = await _dbContext.FunctionSpecifications.ToListAsync(ct);
            clearedFunctionSpecs = functionSpecs.Count;
            _dbContext.FunctionSpecifications.RemoveRange(functionSpecs);
            _logger.LogInformation("🗑️ Marked {Count} function specifications for deletion", clearedFunctionSpecs);
        }

        if (table?.ToLower() == "all" || table?.ToLower() == "codeunits")
        {
            var codeUnits = await _dbContext.CodeUnits.ToListAsync(ct);
            clearedCodeUnits = codeUnits.Count;
            _dbContext.CodeUnits.RemoveRange(codeUnits);
            _logger.LogInformation("🗑️ Marked {Count} code units for deletion", clearedCodeUnits);
        }

        await _dbContext.SaveChangesAsync(ct);
        
        response.Message = $"Cleared database content. CodeUnits: {clearedCodeUnits}, FunctionSpecs: {clearedFunctionSpecs}";
        
        _logger.LogInformation("✅ Database cleared - CodeUnits: {CodeUnits}, FunctionSpecs: {FunctionSpecs}", 
            clearedCodeUnits, clearedFunctionSpecs);
    }
}