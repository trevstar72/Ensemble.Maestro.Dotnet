using System.Text.Json;
using System.Text.RegularExpressions;
using Ensemble.Maestro.Dotnet.Core.Agents;
using Ensemble.Maestro.Dotnet.Core.Data;
using Ensemble.Maestro.Dotnet.Core.Data.Entities;
using Microsoft.Extensions.Logging;

namespace Ensemble.Maestro.Dotnet.Core.Services;

/// <summary>
/// Service implementation for storing designer agent outputs across SQL Server, Neo4j, and Elasticsearch
/// </summary>
public class DesignerOutputStorageService : IDesignerOutputStorageService
{
    private readonly MaestroDbContext _dbContext;
    private readonly ICrossReferenceService _crossReferenceService;
    private readonly INeo4jService _neo4jService;
    private readonly IElasticsearchService _elasticsearchService;
    private readonly ILLMService _llmService;
    private readonly ILogger<DesignerOutputStorageService> _logger;

    public DesignerOutputStorageService(
        MaestroDbContext dbContext,
        ICrossReferenceService crossReferenceService,
        INeo4jService neo4jService,
        IElasticsearchService elasticsearchService,
        ILLMService llmService,
        ILogger<DesignerOutputStorageService> logger)
    {
        _dbContext = dbContext;
        _crossReferenceService = crossReferenceService;
        _neo4jService = neo4jService;
        _elasticsearchService = elasticsearchService;
        _llmService = llmService;
        _logger = logger;
    }

    public async Task<DesignerOutputStorageResult> StoreDesignerOutputAsync(
        AgentExecutionContext context,
        AgentExecutionResult result,
        Guid agentExecutionId,
        string agentType,
        string agentName,
        CancellationToken cancellationToken = default)
    {
        var storageResult = new DesignerOutputStorageResult();

        try
        {
            _logger.LogInformation("Storing designer output for agent execution {AgentExecutionId}", agentExecutionId);

            // 1. Create cross-reference entry first
            var crossRef = await _crossReferenceService.CreateCrossReferenceAsync(
                "DesignerOutput", 
                new { AgentType = agentType, ProjectId = context.ProjectId },
                cancellationToken);
            
            storageResult.CrossReferenceId = crossRef.PrimaryId;

            // 2. Parse function specifications from output
            var functionSpecs = await ParseFunctionSpecificationsAsync(result.OutputResponse, context, cancellationToken);
            
            // 3. Extract code units
            var codeUnits = await ExtractCodeUnitsAsync(result.OutputResponse, functionSpecs, context, cancellationToken);

            // 4. Create and store DesignerOutput entity in SQL Server
            var designerOutput = new DesignerOutput
            {
                Id = Guid.NewGuid(),
                CrossReferenceId = crossRef.PrimaryId,
                ProjectId = context.ProjectId,
                PipelineExecutionId = context.PipelineExecutionId,
                AgentExecutionId = agentExecutionId,
                AgentType = agentType,
                AgentName = agentName,
                Title = ExtractTitle(result.OutputResponse) ?? "System Design",
                Description = ExtractDescription(result.OutputResponse),
                TargetLanguage = context.TargetLanguage,
                DeploymentTarget = context.DeploymentTarget,
                MarkdownOutput = result.OutputResponse,
                StructuredData = SerializeStructuredData(result, agentType),
                ArchitectureOverview = ExtractSection(result.OutputResponse, "architecture"),
                ComponentSpecs = ExtractSection(result.OutputResponse, "component"),
                ApiSpecs = ExtractSection(result.OutputResponse, "api"),
                DatabaseSpecs = ExtractSection(result.OutputResponse, "database"),
                UiSpecs = ExtractSection(result.OutputResponse, "ui"),
                SecuritySpecs = ExtractSection(result.OutputResponse, "security"),
                PerformanceSpecs = ExtractSection(result.OutputResponse, "performance"),
                IntegrationSpecs = ExtractSection(result.OutputResponse, "integration"),
                TestingSpecs = ExtractSection(result.OutputResponse, "testing"),
                DeploymentSpecs = ExtractSection(result.OutputResponse, "deployment"),
                FunctionSpecsCount = functionSpecs.Count,
                ComplexityRating = CalculateComplexityRating(result.OutputResponse, functionSpecs),
                EstimatedHours = CalculateEstimatedHours(functionSpecs, codeUnits),
                QualityScore = result.QualityScore,
                ConfidenceScore = result.ConfidenceScore,
                Status = "Created",
                ProcessingStage = "Parsed",
                InputTokens = result.InputTokens,
                OutputTokens = result.OutputTokens,
                ExecutionCost = result.ExecutionCost,
                Version = 1
            };

            _dbContext.DesignerOutputs.Add(designerOutput);

            // 5. Store function specifications
            var storedFunctionSpecs = new List<FunctionSpecification>();
            foreach (var spec in functionSpecs)
            {
                var funcCrossRef = await _crossReferenceService.CreateCrossReferenceAsync(
                    "FunctionSpecification", 
                    new { FunctionName = spec.FunctionName, CodeUnit = spec.CodeUnit },
                    cancellationToken);

                var functionSpec = new FunctionSpecification
                {
                    Id = Guid.NewGuid(),
                    CrossReferenceId = funcCrossRef.PrimaryId,
                    ProjectId = context.ProjectId,
                    PipelineExecutionId = context.PipelineExecutionId,
                    AgentExecutionId = agentExecutionId,
                    FunctionName = spec.FunctionName,
                    CodeUnit = spec.CodeUnit,
                    Namespace = spec.Namespace,
                    Language = context.TargetLanguage ?? "CSharp",
                    Signature = spec.Signature,
                    Description = spec.Description,
                    InputParameters = spec.InputParameters,
                    ReturnType = spec.ReturnType,
                    Dependencies = spec.Dependencies,
                    BusinessLogic = spec.BusinessLogic,
                    ValidationRules = spec.ValidationRules,
                    ErrorHandling = spec.ErrorHandling,
                    PerformanceRequirements = spec.PerformanceRequirements,
                    SecurityConsiderations = spec.SecurityConsiderations,
                    TestCases = spec.TestCases,
                    ComplexityRating = spec.ComplexityRating,
                    EstimatedMinutes = spec.EstimatedMinutes,
                    Priority = spec.Priority,
                    Status = "Draft",
                    CreatedByAgent = agentType,
                    Version = 1
                };

                _dbContext.FunctionSpecifications.Add(functionSpec);
                storedFunctionSpecs.Add(functionSpec);
            }

            // 6. Store code units
            var storedCodeUnits = new List<CodeUnit>();
            foreach (var unit in codeUnits)
            {
                var unitCrossRef = await _crossReferenceService.CreateCrossReferenceAsync(
                    "CodeUnit",
                    new { Name = unit.Name, UnitType = unit.UnitType },
                    cancellationToken);

                var codeUnit = new CodeUnit
                {
                    Id = Guid.NewGuid(),
                    CrossReferenceId = unitCrossRef.PrimaryId,
                    ProjectId = context.ProjectId,
                    PipelineExecutionId = context.PipelineExecutionId,
                    DesignerOutputId = designerOutput.Id,
                    Name = unit.Name,
                    UnitType = unit.UnitType,
                    Namespace = unit.Namespace,
                    Language = context.TargetLanguage ?? "CSharp",
                    FilePath = unit.FilePath,
                    Description = unit.Description,
                    Inheritance = unit.Inheritance,
                    Dependencies = unit.Dependencies,
                    Fields = unit.Fields,
                    Constructors = unit.Constructors,
                    Patterns = unit.Patterns,
                    Responsibilities = unit.Responsibilities,
                    Integrations = unit.Integrations,
                    SecurityConsiderations = unit.SecurityConsiderations,
                    PerformanceConsiderations = unit.PerformanceConsiderations,
                    TestingStrategy = unit.TestingStrategy,
                    ComplexityRating = unit.ComplexityRating,
                    EstimatedMinutes = unit.EstimatedMinutes,
                    Priority = unit.Priority,
                    Status = "Planned",
                    ProcessingStage = "Extracted",
                    FunctionCount = unit.FunctionNames.Count,
                    SimpleFunctionCount = unit.FunctionNames.Count(fn => 
                        storedFunctionSpecs.Any(fs => fs.FunctionName == fn && fs.ComplexityRating < 4)),
                    ComplexFunctionCount = unit.FunctionNames.Count(fn => 
                        storedFunctionSpecs.Any(fs => fs.FunctionName == fn && fs.ComplexityRating >= 4)),
                    CompletionPercentage = 0,
                    Version = 1
                };

                _dbContext.CodeUnits.Add(codeUnit);
                storedCodeUnits.Add(codeUnit);
            }

            // 7. Save all to SQL Server
            await _dbContext.SaveChangesAsync(cancellationToken);
            
            storageResult.DesignerOutputId = designerOutput.Id;
            storageResult.FunctionSpecificationsStored = storedFunctionSpecs.Count;
            storageResult.CodeUnitsStored = storedCodeUnits.Count;

            // 8. Store in Neo4j (relationships and graph data)
            await StoreInNeo4jAsync(designerOutput, storedFunctionSpecs, storedCodeUnits, crossRef.PrimaryId, cancellationToken);

            // 9. Store in Elasticsearch (full-text search)
            await StoreInElasticsearchAsync(designerOutput, storedFunctionSpecs, storedCodeUnits, crossRef.PrimaryId, cancellationToken);

            // 10. Update cross-reference with database IDs
            crossRef.SqlId = designerOutput.Id;
            await _crossReferenceService.UpdateCrossReferenceAsync(crossRef.PrimaryId, crossRef, cancellationToken);

            storageResult.Success = true;
            
            _logger.LogInformation("Successfully stored designer output: {FunctionSpecs} function specs, {CodeUnits} code units", 
                storageResult.FunctionSpecificationsStored, storageResult.CodeUnitsStored);

            return storageResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to store designer output for agent execution {AgentExecutionId}", agentExecutionId);
            storageResult.Success = false;
            storageResult.ErrorMessage = ex.Message;
            return storageResult;
        }
    }

    public async Task<List<ParsedFunctionSpecification>> ParseFunctionSpecificationsAsync(
        string markdownOutput,
        AgentExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        var functionSpecs = new List<ParsedFunctionSpecification>();

        try
        {
            // Use LLM to extract structured function specifications from markdown
            var extractionPrompt = $@"Extract function specifications from the following designer output. 
Return a JSON array of function specifications with the following structure:
{{
  ""functionName"": ""string"",
  ""codeUnit"": ""string"",
  ""namespace"": ""string"",
  ""signature"": ""string"",
  ""description"": ""string"",
  ""inputParameters"": ""JSON string of parameters"",
  ""returnType"": ""JSON string of return type"",
  ""dependencies"": ""JSON array of dependencies"",
  ""businessLogic"": ""string"",
  ""validationRules"": ""JSON string of validation rules"",
  ""errorHandling"": ""string"",
  ""performanceRequirements"": ""string"",
  ""securityConsiderations"": ""string"",
  ""testCases"": ""JSON array of test cases"",
  ""complexityRating"": number (1-10),
  ""estimatedMinutes"": number,
  ""priority"": ""string (Low/Medium/High)""
}}

Designer Output:
{markdownOutput}";

            var response = await _llmService.GenerateResponseAsync(
                "You are a technical analyst specializing in extracting structured data from technical specifications.",
                extractionPrompt,
                4000,
                0.1f,
                cancellationToken);

            if (response.Success && !string.IsNullOrEmpty(response.Content))
            {
                // Try to extract JSON from the response
                var jsonMatch = Regex.Match(response.Content, @"\[.*\]", RegexOptions.Singleline);
                if (jsonMatch.Success)
                {
                    var functionsJson = jsonMatch.Value;
                    var parsedFunctions = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(functionsJson);
                    
                    if (parsedFunctions != null)
                    {
                        foreach (var func in parsedFunctions)
                        {
                            var spec = new ParsedFunctionSpecification();
                            
                            if (func.TryGetValue("functionName", out var name))
                                spec.FunctionName = name.ToString() ?? "";
                            if (func.TryGetValue("codeUnit", out var unit))
                                spec.CodeUnit = unit.ToString() ?? "";
                            if (func.TryGetValue("namespace", out var ns))
                                spec.Namespace = ns.ToString();
                            if (func.TryGetValue("signature", out var sig))
                                spec.Signature = sig.ToString() ?? "";
                            if (func.TryGetValue("description", out var desc))
                                spec.Description = desc.ToString() ?? "";
                            if (func.TryGetValue("inputParameters", out var input))
                                spec.InputParameters = input.ToString();
                            if (func.TryGetValue("returnType", out var ret))
                                spec.ReturnType = ret.ToString();
                            if (func.TryGetValue("dependencies", out var deps))
                                spec.Dependencies = deps.ToString();
                            if (func.TryGetValue("businessLogic", out var logic))
                                spec.BusinessLogic = logic.ToString();
                            if (func.TryGetValue("validationRules", out var validation))
                                spec.ValidationRules = validation.ToString();
                            if (func.TryGetValue("errorHandling", out var error))
                                spec.ErrorHandling = error.ToString();
                            if (func.TryGetValue("performanceRequirements", out var perf))
                                spec.PerformanceRequirements = perf.ToString();
                            if (func.TryGetValue("securityConsiderations", out var security))
                                spec.SecurityConsiderations = security.ToString();
                            if (func.TryGetValue("testCases", out var tests))
                                spec.TestCases = tests.ToString();
                            if (func.TryGetValue("complexityRating", out var complexity) && int.TryParse(complexity.ToString(), out var complexityInt))
                                spec.ComplexityRating = complexityInt;
                            if (func.TryGetValue("estimatedMinutes", out var minutes) && int.TryParse(minutes.ToString(), out var minutesInt))
                                spec.EstimatedMinutes = minutesInt;
                            if (func.TryGetValue("priority", out var priority))
                                spec.Priority = priority.ToString() ?? "Medium";
                            
                            if (!string.IsNullOrEmpty(spec.FunctionName))
                            {
                                functionSpecs.Add(spec);
                            }
                        }
                    }
                }
            }

            _logger.LogInformation("Parsed {Count} function specifications from designer output", functionSpecs.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse function specifications from markdown output");
        }

        return functionSpecs;
    }

    public async Task<List<ParsedCodeUnit>> ExtractCodeUnitsAsync(
        string markdownOutput,
        List<ParsedFunctionSpecification> functionSpecs,
        AgentExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        var codeUnits = new List<ParsedCodeUnit>();

        try
        {
            // Group function specifications by code unit
            var unitGroups = functionSpecs.GroupBy(f => f.CodeUnit).ToList();

            foreach (var group in unitGroups)
            {
                if (string.IsNullOrEmpty(group.Key)) continue;

                var unit = new ParsedCodeUnit
                {
                    Name = group.Key,
                    UnitType = InferUnitType(group.Key, markdownOutput),
                    Namespace = InferNamespace(group.Key, context.TargetLanguage),
                    Description = ExtractUnitDescription(group.Key, markdownOutput),
                    ComplexityRating = (int)Math.Ceiling(group.Average(f => f.ComplexityRating)),
                    EstimatedMinutes = group.Sum(f => f.EstimatedMinutes ?? 0),
                    Priority = group.Any(f => f.Priority == "High") ? "High" : 
                              group.Any(f => f.Priority == "Medium") ? "Medium" : "Low",
                    FunctionNames = group.Select(f => f.FunctionName).ToList()
                };

                // Set file path based on unit type and target language
                unit.FilePath = GenerateFilePath(unit, context);

                codeUnits.Add(unit);
            }

            _logger.LogInformation("Extracted {Count} code units from function specifications", codeUnits.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extract code units from function specifications");
        }

        return codeUnits;
    }

    #region Private Helper Methods

    private async Task StoreInNeo4jAsync(DesignerOutput designerOutput, List<FunctionSpecification> functionSpecs, 
        List<CodeUnit> codeUnits, Guid crossRefId, CancellationToken cancellationToken)
    {
        try
        {
            // Store relationships and graph structure in Neo4j
            // This is a placeholder - implementation depends on Neo4j service capabilities
            await _neo4jService.StoreDesignerOutputAsync(designerOutput, functionSpecs, codeUnits, crossRefId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to store designer output in Neo4j - continuing with SQL and Elasticsearch");
        }
    }

    private async Task StoreInElasticsearchAsync(DesignerOutput designerOutput, List<FunctionSpecification> functionSpecs, 
        List<CodeUnit> codeUnits, Guid crossRefId, CancellationToken cancellationToken)
    {
        try
        {
            // Store for full-text search in Elasticsearch
            // This is a placeholder - implementation depends on Elasticsearch service capabilities
            await _elasticsearchService.StoreDesignerOutputAsync(designerOutput, functionSpecs, codeUnits, crossRefId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to store designer output in Elasticsearch - continuing with SQL and Neo4j");
        }
    }

    private string? ExtractTitle(string markdown)
    {
        var titleMatch = Regex.Match(markdown, @"^#\s+(.+)$", RegexOptions.Multiline);
        return titleMatch.Success ? titleMatch.Groups[1].Value.Trim() : null;
    }

    private string? ExtractDescription(string markdown)
    {
        // Extract the first paragraph after the title
        var lines = markdown.Split('\n');
        var titleFound = false;
        var description = new List<string>();

        foreach (var line in lines)
        {
            if (line.StartsWith("# "))
            {
                titleFound = true;
                continue;
            }

            if (titleFound)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    if (description.Any()) break;
                    continue;
                }

                if (line.StartsWith("#")) break;
                
                description.Add(line.Trim());
                if (description.Count >= 3) break; // First 3 lines
            }
        }

        return description.Any() ? string.Join(" ", description) : null;
    }

    private string? ExtractSection(string markdown, string sectionName)
    {
        var pattern = $@"#+\s+.*{sectionName}.*?(?=#+|$)";
        var match = Regex.Match(markdown, pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
        return match.Success ? match.Value.Trim() : null;
    }

    private string SerializeStructuredData(AgentExecutionResult result, string agentType)
    {
        var data = new
        {
            AgentType = agentType,
            QualityScore = result.QualityScore,
            ConfidenceScore = result.ConfidenceScore,
            TokenUsage = new { Input = result.InputTokens, Output = result.OutputTokens },
            ExecutionCost = result.ExecutionCost,
            DurationSeconds = result.DurationSeconds,
            Artifacts = result.Artifacts?.Select(a => new { a.Name, a.Type, a.Path, a.Size }).ToList()
        };

        return JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
    }

    private int CalculateComplexityRating(string markdown, List<ParsedFunctionSpecification> functionSpecs)
    {
        var baseComplexity = 3;
        
        // Markdown content complexity
        if (markdown.Length > 5000) baseComplexity += 2;
        if (markdown.Contains("```")) baseComplexity += 1;
        
        // Function specifications complexity
        if (functionSpecs.Any())
        {
            var avgComplexity = functionSpecs.Average(f => f.ComplexityRating);
            baseComplexity = (int)Math.Ceiling((baseComplexity + avgComplexity) / 2);
        }

        return Math.Min(10, baseComplexity);
    }

    private decimal? CalculateEstimatedHours(List<ParsedFunctionSpecification> functionSpecs, List<ParsedCodeUnit> codeUnits)
    {
        var totalMinutes = functionSpecs.Sum(f => f.EstimatedMinutes ?? 0) + codeUnits.Sum(c => c.EstimatedMinutes ?? 0);
        return totalMinutes > 0 ? Math.Round((decimal)totalMinutes / 60, 2) : null;
    }

    private string InferUnitType(string unitName, string markdown)
    {
        var lowerName = unitName.ToLower();
        
        if (lowerName.Contains("service")) return "Service";
        if (lowerName.Contains("controller")) return "Controller";
        if (lowerName.Contains("repository")) return "Repository";
        if (lowerName.Contains("interface") || lowerName.StartsWith("i") && char.IsUpper(unitName[1])) return "Interface";
        if (lowerName.Contains("model") || lowerName.Contains("entity")) return "Entity";
        if (lowerName.Contains("exception")) return "Exception";
        if (lowerName.Contains("helper") || lowerName.Contains("utility")) return "Utility";
        
        return "Class"; // Default
    }

    private string? InferNamespace(string unitName, string? targetLanguage)
    {
        var baseNamespace = targetLanguage switch
        {
            "CSharp" or "C#" => "Ensemble.Maestro.Generated",
            "TypeScript" => "generated",
            "Python" => "generated",
            "Java" => "com.ensemble.maestro.generated",
            _ => "Generated"
        };

        return baseNamespace;
    }

    private string? ExtractUnitDescription(string unitName, string markdown)
    {
        // Look for descriptions near the unit name in the markdown
        var pattern = $@"{Regex.Escape(unitName)}.*?(?:\r?\n)+(.*?)(?:\r?\n|$)";
        var match = Regex.Match(markdown, pattern, RegexOptions.IgnoreCase);
        
        if (match.Success && match.Groups.Count > 1)
        {
            var description = match.Groups[1].Value.Trim();
            if (description.Length > 10 && !description.StartsWith("#"))
            {
                return description.Length > 500 ? description.Substring(0, 500) + "..." : description;
            }
        }

        return $"Auto-generated {unitName} from designer output";
    }

    private string GenerateFilePath(ParsedCodeUnit unit, AgentExecutionContext context)
    {
        var extension = context.TargetLanguage switch
        {
            "CSharp" or "C#" => ".cs",
            "TypeScript" => ".ts",
            "Python" => ".py",
            "Java" => ".java",
            "JavaScript" => ".js",
            _ => ".cs" // Default
        };

        var folder = unit.UnitType switch
        {
            "Controller" => "/Controllers/",
            "Service" => "/Services/",
            "Repository" => "/Repositories/",
            "Entity" => "/Entities/",
            "Interface" => "/Interfaces/",
            "Exception" => "/Exceptions/",
            "Utility" => "/Utilities/",
            _ => "/Generated/"
        };

        return $"{folder}{unit.Name}{extension}";
    }

    #endregion
}