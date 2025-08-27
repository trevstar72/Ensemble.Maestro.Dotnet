using Microsoft.Extensions.Logging;
using Ensemble.Maestro.Dotnet.Core.Services;
using Ensemble.Maestro.Dotnet.Core.Messages;
using System.Text.Json;

namespace Ensemble.Maestro.Dotnet.Core.Agents.Building;

/// <summary>
/// Enhanced BuilderAgent that consumes individual code documents from CUCS and performs actual builds
/// Implements error feedback loop by sending BuilderErrorMessages back to CUCS for bug-fix agent spawning
/// </summary>
public class EnhancedBuilderAgent : BaseAgent
{
    private readonly ICodeDocumentStorageService _codeDocumentStorageService;
    private readonly IMessageCoordinatorService _messageCoordinatorService;
    private readonly IBuildExecutionService _buildExecutionService;

    public EnhancedBuilderAgent(
        ILogger<BaseAgent> logger, 
        ILLMService llmService,
        ICodeDocumentStorageService codeDocumentStorageService,
        IMessageCoordinatorService messageCoordinatorService,
        IBuildExecutionService buildExecutionService) 
        : base(logger, llmService) 
    {
        _codeDocumentStorageService = codeDocumentStorageService;
        _messageCoordinatorService = messageCoordinatorService;
        _buildExecutionService = buildExecutionService;
    }
    
    public override string AgentType => "EnhancedBuilder";
    public override string AgentName => "Enhanced System Builder";
    public override string Priority => "High";
    
    protected override async Task<AgentExecutionResult> ExecuteInternalAsync(AgentExecutionContext context, CancellationToken cancellationToken)
    {
        _logger.LogInformation("🏗️ Enhanced Builder executing build pipeline for project {ProjectId}", context.ProjectId);
        
        try
        {
            // Step 1: Aggregate individual code documents into buildable files
            var aggregationResult = await _codeDocumentStorageService.AggregateDocumentsForBuildAsync(
                context.ProjectId.ToString(), cancellationToken);

            if (!aggregationResult.Success)
            {
                return new AgentExecutionResult
                {
                    Success = false,
                    OutputResponse = $"Failed to aggregate code documents: {aggregationResult.Message}",
                    ErrorMessage = aggregationResult.Message
                };
            }

            _logger.LogInformation("📄 Aggregated {DocumentCount} code documents into {FileCount} buildable files", 
                aggregationResult.TotalDocuments, aggregationResult.AggregatedFiles.Count);

            // Step 2: Attempt to build the aggregated files
            var buildResult = await AttemptBuildAsync(aggregationResult, context, cancellationToken);

            if (buildResult.Success)
            {
                // Step 3a: Build succeeded - send BuilderNotificationMessage
                await SendBuildSuccessNotificationAsync(aggregationResult, buildResult, context, cancellationToken);
                
                return new AgentExecutionResult
                {
                    Success = true,
                    OutputResponse = GenerateBuildSuccessReport(aggregationResult, buildResult),
                    QualityScore = CalculateBuildQualityScore(buildResult),
                    ConfidenceScore = 9, // High confidence for successful builds
                    DurationSeconds = (int)(DateTime.UtcNow - DateTime.UtcNow).TotalSeconds,
                    Artifacts = GenerateBuildArtifacts(aggregationResult, buildResult)
                };
            }
            else
            {
                // Step 3b: Build failed - send BuilderErrorMessages to trigger bug-fix agents
                await SendBuildErrorMessagesAsync(aggregationResult, buildResult, context, cancellationToken);
                
                return new AgentExecutionResult
                {
                    Success = false,
                    OutputResponse = GenerateBuildErrorReport(aggregationResult, buildResult),
                    ErrorMessage = buildResult.ErrorMessage,
                    QualityScore = 3, // Low quality for failed builds
                    ConfidenceScore = 8, // Still confident in error reporting
                    DurationSeconds = (int)(DateTime.UtcNow - DateTime.UtcNow).TotalSeconds
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Enhanced Builder failed with exception for project {ProjectId}", context.ProjectId);
            
            return new AgentExecutionResult
            {
                Success = false,
                OutputResponse = $"Build execution failed: {ex.Message}",
                ErrorMessage = ex.Message,
                QualityScore = 1,
                ConfidenceScore = 5
            };
        }
    }

    private async Task<BuildExecutionResult> AttemptBuildAsync(
        BuildAggregationResult aggregationResult, 
        AgentExecutionContext context, 
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("🔨 Attempting to build {FileCount} aggregated files", aggregationResult.AggregatedFiles.Count);

        try
        {
            // Create temporary build directory
            var buildDirectory = Path.Combine(Path.GetTempPath(), "MaestroBuild", context.ProjectId.ToString());
            Directory.CreateDirectory(buildDirectory);

            // Write aggregated files to build directory
            var writtenFiles = new List<string>();
            foreach (var file in aggregationResult.AggregatedFiles)
            {
                var filePath = Path.Combine(buildDirectory, file.FileName);
                await File.WriteAllTextAsync(filePath, file.Content, cancellationToken);
                writtenFiles.Add(filePath);
                
                _logger.LogInformation("📝 Wrote file {FileName} ({Size} chars)", file.FileName, file.Content.Length);
            }

            // Execute build using the build execution service
            var buildResult = await _buildExecutionService.ExecuteBuildAsync(
                buildDirectory, 
                aggregationResult.Languages.FirstOrDefault() ?? "CSharp",
                cancellationToken);

            return buildResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Build execution failed");
            
            return new BuildExecutionResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                BuildOutput = ex.ToString(),
                ErrorDetails = new List<BuildError>
                {
                    new BuildError
                    {
                        ErrorType = "BuildSystemError",
                        ErrorMessage = ex.Message,
                        Severity = 10
                    }
                }
            };
        }
    }

    private async Task SendBuildSuccessNotificationAsync(
        BuildAggregationResult aggregationResult,
        BuildExecutionResult buildResult,
        AgentExecutionContext context,
        CancellationToken cancellationToken)
    {
        foreach (var file in aggregationResult.AggregatedFiles)
        {
            var notification = new BuilderNotificationMessage
            {
                ProjectId = context.ProjectId.ToString(),
                PipelineExecutionId = context.PipelineExecutionId.ToString(),
                CodeUnitControllerId = "CUCS",
                CodeUnitName = file.CodeUnitName,
                CodeUnitId = file.CodeUnitName, // For now, use name as ID
                Status = "Complete",
                Message = "Build completed successfully",
                TotalFunctions = file.FunctionCount,
                CompletedFunctions = file.FunctionCount,
                FailedFunctions = 0,
                QualityScore = CalculateBuildQualityScore(buildResult),
                TotalCost = 0m, // TODO: Calculate actual build cost
                TotalDurationSeconds = buildResult.BuildDurationSeconds,
                Priority = "High"
            };

            await _messageCoordinatorService.SendBuilderNotificationAsync(notification, cancellationToken);
            
            _logger.LogInformation("✅ Sent build success notification for CodeUnit: {CodeUnitName}", file.CodeUnitName);
        }
    }

    private async Task SendBuildErrorMessagesAsync(
        BuildAggregationResult aggregationResult,
        BuildExecutionResult buildResult,
        AgentExecutionContext context,
        CancellationToken cancellationToken)
    {
        foreach (var error in buildResult.ErrorDetails)
        {
            var errorMessage = new BuilderErrorMessage
            {
                ProjectId = context.ProjectId.ToString(),
                PipelineExecutionId = context.PipelineExecutionId.ToString(),
                CodeUnitName = error.CodeUnitName ?? "Unknown",
                CodeUnitId = error.CodeUnitName ?? "Unknown",
                BuilderAgentId = "EnhancedBuilder",
                ErrorType = error.ErrorType,
                ErrorMessage = error.ErrorMessage,
                ErrorDetails = error.Details,
                StackTrace = error.StackTrace,
                FunctionName = error.FunctionName,
                FunctionSignature = error.FunctionSignature,
                LineNumber = error.LineNumber,
                BuildStage = "Compilation",
                BuildOutput = buildResult.BuildOutput,
                Severity = error.Severity,
                Priority = "High", // Build errors are always high priority
                SuggestedFix = error.SuggestedFix,
                RelatedFunctions = error.RelatedFunctions
            };

            await _messageCoordinatorService.SendBuilderErrorAsync(errorMessage, cancellationToken);
            
            _logger.LogError("❌ Sent build error message for {ErrorType}: {ErrorMessage}", 
                error.ErrorType, error.ErrorMessage);
        }
    }

    private string GenerateBuildSuccessReport(BuildAggregationResult aggregationResult, BuildExecutionResult buildResult)
    {
        return $@"# Build Success Report

## Build Summary
- **Status**: ✅ SUCCESS
- **Files Built**: {aggregationResult.AggregatedFiles.Count}
- **Total Code Documents**: {aggregationResult.TotalDocuments}
- **Code Units**: {aggregationResult.TotalCodeUnits}
- **Languages**: {string.Join(", ", aggregationResult.Languages)}
- **Build Duration**: {buildResult.BuildDurationSeconds} seconds

## Generated Files
{string.Join("\n", aggregationResult.AggregatedFiles.Select(f => 
    $"- **{f.FileName}** ({f.Language}): {f.FunctionCount} functions, {f.TotalSize} chars"))}

## Build Output
```
{buildResult.BuildOutput}
```

## Artifacts Generated
{string.Join("\n", buildResult.GeneratedArtifacts.Select(a => $"- {a}"))}

---
*Generated by Enhanced BuilderAgent at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC*";
    }

    private string GenerateBuildErrorReport(BuildAggregationResult aggregationResult, BuildExecutionResult buildResult)
    {
        return $@"# Build Error Report

## Build Summary
- **Status**: ❌ FAILED
- **Files Attempted**: {aggregationResult.AggregatedFiles.Count}
- **Total Code Documents**: {aggregationResult.TotalDocuments}
- **Code Units**: {aggregationResult.TotalCodeUnits}
- **Languages**: {string.Join(", ", aggregationResult.Languages)}
- **Build Duration**: {buildResult.BuildDurationSeconds} seconds

## Error Details
{string.Join("\n\n", buildResult.ErrorDetails.Select(e => $@"### {e.ErrorType}
- **Message**: {e.ErrorMessage}
- **File**: {e.FileName ?? "Unknown"}
- **Function**: {e.FunctionName ?? "Unknown"}
- **Line**: {e.LineNumber?.ToString() ?? "Unknown"}
- **Severity**: {e.Severity}/10"))}

## Build Output
```
{buildResult.BuildOutput}
```

## Next Steps
BuilderErrorMessages have been sent to CUCS to spawn bug-fix Method Agents for automatic error resolution.

---
*Generated by Enhanced BuilderAgent at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC*";
    }

    private int CalculateBuildQualityScore(BuildExecutionResult buildResult)
    {
        if (!buildResult.Success)
            return Math.Max(1, 5 - buildResult.ErrorDetails.Count);
        
        return 9; // High quality for successful builds
    }

    private List<AgentArtifact> GenerateBuildArtifacts(BuildAggregationResult aggregationResult, BuildExecutionResult buildResult)
    {
        var artifacts = new List<AgentArtifact>
        {
            new AgentArtifact
            {
                Name = "build_report.md",
                Type = "markdown",
                Content = buildResult.Success 
                    ? GenerateBuildSuccessReport(aggregationResult, buildResult)
                    : GenerateBuildErrorReport(aggregationResult, buildResult),
                Path = "/build/build_report.md",
                Size = 1000
            }
        };

        if (buildResult.Success)
        {
            artifacts.AddRange(buildResult.GeneratedArtifacts.Select(artifact => new AgentArtifact
            {
                Name = Path.GetFileName(artifact),
                Type = Path.GetExtension(artifact).TrimStart('.'),
                Content = $"Built artifact: {artifact}",
                Path = $"/build/artifacts/{Path.GetFileName(artifact)}",
                Size = 500
            }));
        }

        return artifacts;
    }
}