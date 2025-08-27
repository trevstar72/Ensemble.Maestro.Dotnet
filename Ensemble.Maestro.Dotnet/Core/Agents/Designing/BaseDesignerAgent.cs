using Microsoft.Extensions.Logging;
using Ensemble.Maestro.Dotnet.Core.Services;

namespace Ensemble.Maestro.Dotnet.Core.Agents.Designing;

/// <summary>
/// Base class for all Designer agents with multi-database storage capabilities
/// </summary>
public abstract class BaseDesignerAgent : BaseAgent
{
    protected readonly IDesignerOutputStorageService _designerOutputStorageService;

    protected BaseDesignerAgent(
        ILogger<BaseAgent> logger,
        ILLMService llmService,
        IDesignerOutputStorageService designerOutputStorageService) 
        : base(logger, llmService)
    {
        _designerOutputStorageService = designerOutputStorageService;
    }

    /// <summary>
    /// Override PostExecuteAsync to store designer output in all three databases
    /// </summary>
    protected override async Task PostExecuteAsync(
        AgentExecutionContext context, 
        AgentExecutionResult result, 
        CancellationToken cancellationToken)
    {
        await base.PostExecuteAsync(context, result, cancellationToken);

        // Only store output if the execution was successful and produced content
        if (result.Success && !string.IsNullOrEmpty(result.OutputResponse))
        {
            try
            {
                _logger.LogInformation("Storing designer agent output to multi-database system for agent {AgentType}", AgentType);

                var storageResult = await _designerOutputStorageService.StoreDesignerOutputAsync(
                    context,
                    result,
                    context.ExecutionId,
                    AgentType,
                    AgentName,
                    cancellationToken);

                if (storageResult.Success)
                {
                    _logger.LogInformation("Successfully stored designer output: {FunctionSpecs} function specs, {CodeUnits} code units", 
                        storageResult.FunctionSpecificationsStored, storageResult.CodeUnitsStored);

                    // Add storage information to the result metadata
                    if (result.Metadata == null)
                        result.Metadata = new Dictionary<string, object>();

                    result.Metadata["StorageResult"] = new
                    {
                        Success = storageResult.Success,
                        CrossReferenceId = storageResult.CrossReferenceId,
                        DesignerOutputId = storageResult.DesignerOutputId,
                        FunctionSpecsStored = storageResult.FunctionSpecificationsStored,
                        CodeUnitsStored = storageResult.CodeUnitsStored
                    };

                    // Log warnings if any
                    foreach (var warning in storageResult.Warnings)
                    {
                        _logger.LogWarning("Designer output storage warning: {Warning}", warning);
                    }
                }
                else
                {
                    _logger.LogError("Failed to store designer output: {ErrorMessage}", storageResult.ErrorMessage);
                    
                    // Add error to result but don't fail the agent execution
                    if (result.Metadata == null)
                        result.Metadata = new Dictionary<string, object>();

                    result.Metadata["StorageError"] = storageResult.ErrorMessage ?? "Unknown storage error";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error storing designer output for agent {AgentType}", AgentType);
                
                // Log the error but don't fail the agent execution
                if (result.Metadata == null)
                    result.Metadata = new Dictionary<string, object>();

                result.Metadata["StorageException"] = ex.Message;
            }
        }
        else
        {
            _logger.LogWarning("Skipping designer output storage - execution was not successful or produced no content");
        }
    }

    /// <summary>
    /// Override GetExpectedSectionsForAgent for designer agents
    /// </summary>
    protected override string[] GetExpectedSectionsForAgent()
    {
        return AgentType switch
        {
            "Designer" => new[] { "design", "architecture", "component", "api", "database", "security" },
            "UIDesigner" => new[] { "ui", "design", "component", "style", "responsive", "accessibility" },
            "APIDesigner" => new[] { "api", "endpoint", "schema", "authentication", "documentation", "openapi" },
            _ => new[] { "design", "specification", "component", "architecture" }
        };
    }
}