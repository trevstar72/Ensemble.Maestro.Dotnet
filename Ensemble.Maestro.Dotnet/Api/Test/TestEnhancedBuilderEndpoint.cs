using FastEndpoints;
using Ensemble.Maestro.Dotnet.Core.Agents;
using Ensemble.Maestro.Dotnet.Core.Agents.Building;
using Ensemble.Maestro.Dotnet.Core.Services;

namespace Ensemble.Maestro.Dotnet.Api.Test;

public class TestEnhancedBuilderRequest
{
    public string ProjectId { get; set; } = "test-project-" + Guid.NewGuid().ToString("N")[..8];
    public bool CreateSampleDocuments { get; set; } = true;
    public string Language { get; set; } = "CSharp";
}

public class TestEnhancedBuilderResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string ProjectId { get; set; } = string.Empty;
    public int DocumentsCreated { get; set; }
    public int FilesAggregated { get; set; }
    public bool BuildSucceeded { get; set; }
    public int BuildDurationSeconds { get; set; }
    public List<string> BuildErrors { get; set; } = new();
    public List<string> GeneratedArtifacts { get; set; } = new();
    public string? BuildOutput { get; set; }
    public string? Error { get; set; }
}

/// <summary>
/// Test endpoint for the Enhanced Builder Agent that consumes individual code documents
/// and performs actual build operations with error feedback
/// </summary>
public class TestEnhancedBuilderEndpoint : Endpoint<TestEnhancedBuilderRequest, TestEnhancedBuilderResponse>
{
    private readonly EnhancedBuilderAgent _enhancedBuilderAgent;
    private readonly ICodeDocumentStorageService _codeDocumentStorageService;
    private readonly ILogger<TestEnhancedBuilderEndpoint> _logger;

    public TestEnhancedBuilderEndpoint(
        EnhancedBuilderAgent enhancedBuilderAgent,
        ICodeDocumentStorageService codeDocumentStorageService,
        ILogger<TestEnhancedBuilderEndpoint> logger)
    {
        _enhancedBuilderAgent = enhancedBuilderAgent;
        _codeDocumentStorageService = codeDocumentStorageService;
        _logger = logger;
    }

    public override void Configure()
    {
        Post("/api/test/enhanced-builder");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Test Enhanced Builder Agent";
            s.Description = "Tests the complete Enhanced Builder flow: aggregate code documents → build → error feedback loop";
        });
    }

    public override async Task HandleAsync(TestEnhancedBuilderRequest req, CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("🧪 Starting Enhanced Builder test for project: {ProjectId}", req.ProjectId);

            var documentsCreated = 0;

            // Step 1: Create sample code documents if requested
            if (req.CreateSampleDocuments)
            {
                documentsCreated = await CreateSampleCodeDocumentsAsync(req.ProjectId, req.Language, ct);
                _logger.LogInformation("📄 Created {Count} sample code documents", documentsCreated);
            }

            // Step 2: Create execution context for the Enhanced Builder
            var context = new AgentExecutionContext
            {
                ExecutionId = Guid.NewGuid(),
                ProjectId = Guid.Parse(req.ProjectId.Replace("test-project-", "").PadRight(32, '0')[..32]),
                PipelineExecutionId = Guid.NewGuid(),
                StageExecutionId = Guid.NewGuid(),
                Stage = "Build",
                InputPrompt = "Build the aggregated code documents into executable artifacts",
                TargetLanguage = req.Language,
                DeploymentTarget = "Development"
            };

            // Step 3: Execute the Enhanced Builder
            _logger.LogInformation("🔨 Executing Enhanced Builder Agent...");
            var result = await _enhancedBuilderAgent.ExecuteAsync(context, ct);

            // Step 4: Analyze build results
            var buildSucceeded = result.Success;
            var buildErrors = new List<string>();
            var generatedArtifacts = new List<string>();

            if (result.Artifacts != null)
            {
                generatedArtifacts.AddRange(result.Artifacts.Select(a => a.Name));
            }

            if (!buildSucceeded && !string.IsNullOrEmpty(result.ErrorMessage))
            {
                buildErrors.Add(result.ErrorMessage);
            }

            await Send.OkAsync(new TestEnhancedBuilderResponse
            {
                Success = true, // Test succeeded regardless of build result
                Message = buildSucceeded 
                    ? "Enhanced Builder test completed successfully - build succeeded!" 
                    : "Enhanced Builder test completed - build failed but error feedback was triggered",
                ProjectId = req.ProjectId,
                DocumentsCreated = documentsCreated,
                FilesAggregated = await GetAggregatedFilesCountAsync(req.ProjectId, ct),
                BuildSucceeded = buildSucceeded,
                BuildDurationSeconds = result.DurationSeconds,
                BuildErrors = buildErrors,
                GeneratedArtifacts = generatedArtifacts,
                BuildOutput = result.OutputResponse
            }, ct);

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Enhanced Builder test failed");

            await Send.ResponseAsync(new TestEnhancedBuilderResponse
            {
                Success = false,
                Message = "Enhanced Builder test failed with exception",
                Error = ex.Message,
                ProjectId = req.ProjectId
            }, 500, ct);
        }
    }

    private async Task<int> CreateSampleCodeDocumentsAsync(string projectId, string language, CancellationToken ct)
    {
        var documents = language.ToLower() switch
        {
            "csharp" or "c#" => CreateCSharpSampleDocuments(),
            "typescript" => CreateTypeScriptSampleDocuments(),
            "python" => CreatePythonSampleDocuments(),
            _ => CreateCSharpSampleDocuments() // Default to C#
        };

        var count = 0;
        foreach (var (codeUnit, function, content) in documents)
        {
            var mockResult = new AgentExecutionResult
            {
                Success = true,
                OutputResponse = content,
                QualityScore = 8,
                ConfidenceScore = 9,
                InputTokens = 200,
                OutputTokens = 500,
                ExecutionCost = 0.12m
            };

            await _codeDocumentStorageService.StoreCodeDocumentAsync(projectId, codeUnit, function, mockResult, ct);
            count++;
        }

        return count;
    }

    private async Task<int> GetAggregatedFilesCountAsync(string projectId, CancellationToken ct)
    {
        try
        {
            var aggregationResult = await _codeDocumentStorageService.AggregateDocumentsForBuildAsync(projectId, ct);
            return aggregationResult.Success ? aggregationResult.AggregatedFiles.Count : 0;
        }
        catch
        {
            return 0;
        }
    }

    private List<(string codeUnit, string function, string content)> CreateCSharpSampleDocuments()
    {
        return new List<(string, string, string)>
        {
            ("Calculator", "Add", @"public double Add(double a, double b)
{
    if (double.IsInfinity(a) || double.IsInfinity(b))
        throw new ArgumentException(""Infinite values not supported"");
    
    return a + b;
}"),
            ("Calculator", "Subtract", @"public double Subtract(double a, double b)
{
    if (double.IsInfinity(a) || double.IsInfinity(b))
        throw new ArgumentException(""Infinite values not supported"");
    
    return a - b;
}"),
            ("Calculator", "Multiply", @"public double Multiply(double a, double b)
{
    if (double.IsInfinity(a) || double.IsInfinity(b))
        throw new ArgumentException(""Infinite values not supported"");
    
    var result = a * b;
    if (double.IsInfinity(result))
        throw new OverflowException(""Multiplication result is infinite"");
    
    return result;
}"),
            ("Calculator", "Divide", @"public double Divide(double a, double b)
{
    if (b == 0)
        throw new DivideByZeroException(""Cannot divide by zero"");
    
    if (double.IsInfinity(a) || double.IsInfinity(b))
        throw new ArgumentException(""Infinite values not supported"");
    
    return a / b;
}")
        };
    }

    private List<(string codeUnit, string function, string content)> CreateTypeScriptSampleDocuments()
    {
        return new List<(string, string, string)>
        {
            ("Calculator", "add", @"add(a: number, b: number): number {
    if (!isFinite(a) || !isFinite(b)) {
        throw new Error('Infinite values not supported');
    }
    return a + b;
}"),
            ("Calculator", "subtract", @"subtract(a: number, b: number): number {
    if (!isFinite(a) || !isFinite(b)) {
        throw new Error('Infinite values not supported');
    }
    return a - b;
}")
        };
    }

    private List<(string codeUnit, string function, string content)> CreatePythonSampleDocuments()
    {
        return new List<(string, string, string)>
        {
            ("Calculator", "add", @"def add(self, a: float, b: float) -> float:
    if not (math.isfinite(a) and math.isfinite(b)):
        raise ValueError('Infinite values not supported')
    return a + b"),
            ("Calculator", "subtract", @"def subtract(self, a: float, b: float) -> float:
    if not (math.isfinite(a) and math.isfinite(b)):
        raise ValueError('Infinite values not supported')
    return a - b")
        };
    }
}