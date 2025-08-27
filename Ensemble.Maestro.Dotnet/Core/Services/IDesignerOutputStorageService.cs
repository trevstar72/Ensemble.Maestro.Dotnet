using Ensemble.Maestro.Dotnet.Core.Agents;
using Ensemble.Maestro.Dotnet.Core.Data.Entities;

namespace Ensemble.Maestro.Dotnet.Core.Services;

/// <summary>
/// Service for storing designer agent outputs across SQL Server, Neo4j, and Elasticsearch
/// </summary>
public interface IDesignerOutputStorageService
{
    /// <summary>
    /// Store designer agent output in all three databases with cross-reference coordination
    /// </summary>
    Task<DesignerOutputStorageResult> StoreDesignerOutputAsync(
        AgentExecutionContext context,
        AgentExecutionResult result,
        Guid agentExecutionId,
        string agentType,
        string agentName,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Parse designer output and extract function specifications
    /// </summary>
    Task<List<ParsedFunctionSpecification>> ParseFunctionSpecificationsAsync(
        string markdownOutput,
        AgentExecutionContext context,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Extract code units from designer output
    /// </summary>
    Task<List<ParsedCodeUnit>> ExtractCodeUnitsAsync(
        string markdownOutput,
        List<ParsedFunctionSpecification> functionSpecs,
        AgentExecutionContext context,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of storing designer output across all databases
/// </summary>
public class DesignerOutputStorageResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public Guid? CrossReferenceId { get; set; }
    public Guid? DesignerOutputId { get; set; }
    public int FunctionSpecificationsStored { get; set; }
    public int CodeUnitsStored { get; set; }
    public List<string> Warnings { get; set; } = new();
}

/// <summary>
/// Parsed function specification from designer output
/// </summary>
public class ParsedFunctionSpecification
{
    public string FunctionName { get; set; } = string.Empty;
    public string CodeUnit { get; set; } = string.Empty;
    public string? Namespace { get; set; }
    public string Signature { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? InputParameters { get; set; }
    public string? ReturnType { get; set; }
    public string? Dependencies { get; set; }
    public string? BusinessLogic { get; set; }
    public string? ValidationRules { get; set; }
    public string? ErrorHandling { get; set; }
    public string? PerformanceRequirements { get; set; }
    public string? SecurityConsiderations { get; set; }
    public string? TestCases { get; set; }
    public int ComplexityRating { get; set; } = 5;
    public int? EstimatedMinutes { get; set; }
    public string Priority { get; set; } = "Medium";
}

/// <summary>
/// Parsed code unit from designer output
/// </summary>
public class ParsedCodeUnit
{
    public string Name { get; set; } = string.Empty;
    public string UnitType { get; set; } = string.Empty;
    public string? Namespace { get; set; }
    public string? FilePath { get; set; }
    public string? Description { get; set; }
    public string? Inheritance { get; set; }
    public string? Dependencies { get; set; }
    public string? Fields { get; set; }
    public string? Constructors { get; set; }
    public string? Patterns { get; set; }
    public string? Responsibilities { get; set; }
    public string? Integrations { get; set; }
    public string? SecurityConsiderations { get; set; }
    public string? PerformanceConsiderations { get; set; }
    public string? TestingStrategy { get; set; }
    public int ComplexityRating { get; set; } = 5;
    public int? EstimatedMinutes { get; set; }
    public string Priority { get; set; } = "Medium";
    public List<string> FunctionNames { get; set; } = new();
}