using Microsoft.Extensions.Logging;
using Ensemble.Maestro.Dotnet.Core.Services;
using Ensemble.Maestro.Dotnet.Core.Data.Entities;
using Ensemble.Maestro.Dotnet.Core.Messages;
using System.Text.Json;

namespace Ensemble.Maestro.Dotnet.Core.Agents.Swarming;

/// <summary>
/// Agent responsible for managing a specific code unit (class/module) and spawning Method Agents for individual functions
/// This agent analyzes complexity, prioritizes functions, and coordinates Method Agent execution
/// </summary>
public class CodeUnitControllerAgent : BaseAgent
{
    private readonly IMessageCoordinatorService _messageCoordinator;
    private readonly ISwarmConfigurationService _swarmConfig;
    private readonly IAgentFactory _agentFactory;
    private readonly ICrossReferenceService _crossReference;

    public CodeUnitControllerAgent(
        ILogger<CodeUnitControllerAgent> logger, 
        ILLMService llmService,
        IMessageCoordinatorService messageCoordinator,
        ISwarmConfigurationService swarmConfig,
        IAgentFactory agentFactory,
        ICrossReferenceService crossReference)
        : base(logger, llmService)
    {
        _messageCoordinator = messageCoordinator;
        _swarmConfig = swarmConfig;
        _agentFactory = agentFactory;
        _crossReference = crossReference;
    }

    public override string AgentType => "CodeUnitController";
    public override string AgentName => "Code Unit Controller";
    public override string Priority => "High";

    /// <summary>
    /// Validate that we have a code unit assignment
    /// </summary>
    public override bool CanExecute(AgentExecutionContext context)
    {
        return base.CanExecute(context) && 
               context.Metadata.ContainsKey("CodeUnitId") &&
               !string.IsNullOrEmpty(context.Metadata["CodeUnitId"]?.ToString());
    }

    /// <summary>
    /// Main execution logic for managing a code unit - orchestration only, no code generation
    /// </summary>
    protected override async Task<AgentExecutionResult> ExecuteInternalAsync(
        AgentExecutionContext context, 
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Code Unit Controller starting orchestration - no code generation");
            
            // Get CodeUnitAssignmentMessage from context
            var codeUnitAssignment = GetCodeUnitAssignmentFromContext(context);
            if (codeUnitAssignment == null)
            {
                return AgentExecutionResult.Failure("No CodeUnitAssignmentMessage found in context");
            }

            _logger.LogInformation("Managing Code Unit: {CodeUnitName} with {FunctionCount} functions", 
                codeUnitAssignment.Name, codeUnitAssignment.Functions.Count);
            
            // Step 1: Analyze work types and spawn Method Agents for ALL functions
            var spawnResults = await SpawnMethodAgentsForAllFunctionsAsync(codeUnitAssignment, context, cancellationToken);
            
            // Step 2: Monitor message queue and Method Agent completion
            await MonitorQueueAndAgentCompletion(codeUnitAssignment, spawnResults, context, cancellationToken);
            
            // Step 3: When queue is empty, send message to Builder queue
            var builderSuccess = await NotifyBuilderWhenComplete(codeUnitAssignment, context, cancellationToken);
            
            return new AgentExecutionResult
            {
                Success = builderSuccess,
                OutputResponse = $"Code Unit Controller completed orchestration for {codeUnitAssignment.Name}. Spawned {spawnResults.Count} Method Agents. Builder notification sent.",
                QualityScore = builderSuccess ? 90 : 50,
                ConfidenceScore = builderSuccess ? 95 : 60,
                DurationSeconds = (int)(DateTime.UtcNow - context.StartTime).TotalSeconds,
                Metadata = new Dictionary<string, object>
                {
                    { "CodeUnitId", codeUnitAssignment.CodeUnitId },
                    { "CodeUnitName", codeUnitAssignment.Name },
                    { "SpawnedAgentCount", spawnResults.Count },
                    { "TotalFunctions", codeUnitAssignment.Functions.Count },
                    { "BuilderNotified", builderSuccess }
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Code Unit Controller orchestration");
            return AgentExecutionResult.Failure($"Code Unit Controller orchestration failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Get CodeUnitAssignmentMessage from execution context
    /// </summary>
    private CodeUnitAssignmentMessage? GetCodeUnitAssignmentFromContext(AgentExecutionContext context)
    {
        try
        {
            // The CodeUnitAssignmentMessage should be passed in context metadata or input
            if (context.Metadata.TryGetValue("CodeUnitAssignment", out var assignmentObj))
            {
                if (assignmentObj is CodeUnitAssignmentMessage assignment)
                    return assignment;
                
                if (assignmentObj is string assignmentJson)
                    return JsonSerializer.Deserialize<CodeUnitAssignmentMessage>(assignmentJson);
            }
            
            // Try to parse from InputPrompt if it contains JSON
            if (context.InputPrompt?.StartsWith("{") == true)
            {
                return JsonSerializer.Deserialize<CodeUnitAssignmentMessage>(context.InputPrompt);
            }
            
            _logger.LogWarning("Could not find CodeUnitAssignmentMessage in context");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing CodeUnitAssignmentMessage from context");
            return null;
        }
    }

    /// <summary>
    /// Spawn Method Agents for ALL functions (no direct implementation)
    /// </summary>
    private async Task<List<string>> SpawnMethodAgentsForAllFunctionsAsync(
        CodeUnitAssignmentMessage codeUnitAssignment,
        AgentExecutionContext context,
        CancellationToken cancellationToken)
    {
        var spawnedAgentIds = new List<string>();
        
        foreach (var function in codeUnitAssignment.Functions)
        {
            try
            {
                // Determine work type based on function characteristics
                var workType = DetermineWorkType(function);
                
                _logger.LogInformation("Spawning Method Agent for function {FunctionName} (work type: {WorkType})", 
                    function.FunctionName, workType);

                // Create FunctionAssignmentMessage for the Method Agent
                var functionAssignment = new FunctionAssignmentMessage
                {
                    AssignmentId = Guid.NewGuid().ToString("N"),
                    FunctionSpecificationId = function.FunctionSpecificationId,
                    FunctionName = function.FunctionName,
                    CodeUnit = function.CodeUnit,
                    Signature = function.Signature,
                    Description = function.Description,
                    BusinessLogic = function.BusinessLogic,
                    ValidationRules = function.ValidationRules,
                    ErrorHandling = function.ErrorHandling,
                    SecurityConsiderations = function.SecurityConsiderations,
                    TestCases = function.TestCases,
                    ComplexityRating = function.ComplexityRating,
                    EstimatedMinutes = function.EstimatedMinutes,
                    Priority = function.Priority,
                    TargetLanguage = function.TargetLanguage
                };

                // Check swarm capacity before spawning
                var capacityCheck = await _swarmConfig.CheckSpawnCapacityAsync(
                    "MethodAgent", context.ProjectId.ToString(), cancellationToken);
                
                if (!capacityCheck.CanSpawn)
                {
                    _logger.LogWarning("Cannot spawn Method Agent for function {FunctionName}: {Reason}. Adding to queue for later processing.", 
                        function.FunctionName, capacityCheck.Reason);
                    // In a full implementation, this would be queued for later processing
                    continue;
                }

                // Send function assignment to spawn Method Agent
                var success = await _messageCoordinator.SendFunctionAssignmentAsync(functionAssignment, cancellationToken);
                
                if (success)
                {
                    spawnedAgentIds.Add(functionAssignment.AssignmentId);
                    _logger.LogInformation("Successfully spawned Method Agent for function {FunctionName} with assignment ID {AssignmentId}", 
                        function.FunctionName, functionAssignment.AssignmentId);
                }
                else
                {
                    _logger.LogError("Failed to spawn Method Agent for function {FunctionName}", function.FunctionName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error spawning Method Agent for function {FunctionName}", function.FunctionName);
            }
        }
        
        _logger.LogInformation("Code Unit Controller spawned {SpawnedCount} Method Agents for {TotalFunctions} functions", 
            spawnedAgentIds.Count, codeUnitAssignment.Functions.Count);
        
        return spawnedAgentIds;
    }

    /// <summary>
    /// Determine work type based on function characteristics (blazor ui, database, etc.)
    /// </summary>
    private string DetermineWorkType(FunctionAssignmentMessage function)
    {
        var description = function.Description?.ToLower() ?? "";
        var functionName = function.FunctionName?.ToLower() ?? "";
        
        // Analyze function characteristics to determine agent type needed
        if (description.Contains("database") || description.Contains("repository") || 
            description.Contains("query") || description.Contains("sql") ||
            functionName.Contains("get") || functionName.Contains("save") || 
            functionName.Contains("delete") || functionName.Contains("update"))
        {
            return "database";
        }
        
        if (description.Contains("ui") || description.Contains("blazor") || 
            description.Contains("component") || description.Contains("page") ||
            description.Contains("view") || description.Contains("render"))
        {
            return "blazor_ui";
        }
        
        if (description.Contains("api") || description.Contains("endpoint") || 
            description.Contains("controller") || description.Contains("service") ||
            functionName.Contains("controller") || functionName.Contains("api"))
        {
            return "api";
        }
        
        if (description.Contains("validation") || description.Contains("authenticate") || 
            description.Contains("authorize") || description.Contains("security"))
        {
            return "security";
        }
        
        if (description.Contains("test") || description.Contains("unit test") || 
            description.Contains("integration test"))
        {
            return "testing";
        }
        
        // Default to general business logic
        return "business_logic";
    }

    /// <summary>
    /// Monitor message queue and Method Agent completion
    /// </summary>
    private async Task MonitorQueueAndAgentCompletion(
        CodeUnitAssignmentMessage codeUnitAssignment,
        List<string> spawnedAgentIds,
        AgentExecutionContext context,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting queue monitoring for Code Unit {CodeUnitName}", codeUnitAssignment.Name);
        
        try
        {
            var completedAgents = new HashSet<string>();
            var maxWaitTime = TimeSpan.FromMinutes(30); // Maximum wait time for all agents to complete
            var startTime = DateTime.UtcNow;
            
            // Monitor agent completion through message subscription
            var completionSubscription = await _messageCoordinator.SubscribeToCompletionsAsync(cancellationToken);
            await foreach (var completionMessage in completionSubscription)
            {
                // Check if this completion is for one of our spawned agents
                if (spawnedAgentIds.Contains(completionMessage.RequestId))
                {
                    completedAgents.Add(completionMessage.RequestId);
                    _logger.LogInformation("Method Agent {AgentId} completed for function {FunctionName}. Status: {Success}", 
                        completionMessage.AgentId, completionMessage.RequestId, 
                        completionMessage.Success ? "Success" : "Failed");
                    
                    // Check if all agents have completed
                    if (completedAgents.Count >= spawnedAgentIds.Count)
                    {
                        _logger.LogInformation("All {TotalAgents} Method Agents have completed for Code Unit {CodeUnitName}", 
                            spawnedAgentIds.Count, codeUnitAssignment.Name);
                        break;
                    }
                }
                
                // Handle Builder error messages that might require spawning new agents
                if (completionMessage.AgentType == "Builder" && !completionMessage.Success && 
                    !string.IsNullOrEmpty(completionMessage.ErrorMessage))
                {
                    await HandleBuilderError(completionMessage.ErrorMessage, codeUnitAssignment, context, cancellationToken);
                }
                
                // Check timeout
                if (DateTime.UtcNow - startTime > maxWaitTime)
                {
                    _logger.LogWarning("Timeout reached waiting for Method Agents to complete. Completed: {CompletedCount}/{TotalCount}", 
                        completedAgents.Count, spawnedAgentIds.Count);
                    break;
                }
            }
            
            _logger.LogInformation("Queue monitoring completed for Code Unit {CodeUnitName}. Final status: {CompletedCount}/{TotalCount} agents completed", 
                codeUnitAssignment.Name, completedAgents.Count, spawnedAgentIds.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during queue monitoring for Code Unit {CodeUnitName}", codeUnitAssignment.Name);
        }
    }

    /// <summary>
    /// Notify Builder when all Method Agents complete (queue is empty)
    /// </summary>
    private async Task<bool> NotifyBuilderWhenComplete(
        CodeUnitAssignmentMessage codeUnitAssignment,
        AgentExecutionContext context,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Sending completion notification to Builder queue for Code Unit {CodeUnitName}", 
            codeUnitAssignment.Name);
        
        try
        {
            // Create a message to notify Builder that this Code Unit is ready for build processing
            var builderMessage = new Dictionary<string, object>
            {
                { "MessageType", "CodeUnitComplete" },
                { "CodeUnitId", codeUnitAssignment.CodeUnitId },
                { "CodeUnitName", codeUnitAssignment.Name },
                { "ProjectId", context.ProjectId },
                { "PipelineExecutionId", context.PipelineExecutionId },
                { "CompletedAt", DateTime.UtcNow },
                { "FunctionCount", codeUnitAssignment.Functions.Count },
                { "ControllerAgentId", context.ExecutionId }
            };
            
            // Send message to Builder queue (this would be implemented in MessageCoordinatorService)
            // For now, using workload distribution as a placeholder
            var workloadMessage = new WorkloadDistributionMessage
            {
                ProjectId = context.ProjectId.ToString(),
                Strategy = "BuilderNotification",
                PreferredBatchSize = 1,
                MaxConcurrency = 1,
                CreatedAt = DateTime.UtcNow
            };
            
            var success = await _messageCoordinator.DistributeWorkloadAsync(workloadMessage, cancellationToken);
            
            if (success)
            {
                _logger.LogInformation("Successfully notified Builder queue for Code Unit {CodeUnitName}", 
                    codeUnitAssignment.Name);
                return true;
            }
            else
            {
                _logger.LogError("Failed to notify Builder queue for Code Unit {CodeUnitName}", 
                    codeUnitAssignment.Name);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error notifying Builder queue for Code Unit {CodeUnitName}", 
                codeUnitAssignment.Name);
            return false;
        }
    }

    /// <summary>
    /// Handle Builder error messages by spawning new Method Agents to fix bugs
    /// </summary>
    private async Task HandleBuilderError(
        string errorMessage,
        CodeUnitAssignmentMessage codeUnitAssignment,
        AgentExecutionContext context,
        CancellationToken cancellationToken)
    {
        _logger.LogWarning("Received Builder error for Code Unit {CodeUnitName}: {ErrorMessage}", 
            codeUnitAssignment.Name, errorMessage);
        
        try
        {
            // Create a new function assignment for bug fixing
            var bugFixAssignment = new FunctionAssignmentMessage
            {
                AssignmentId = Guid.NewGuid().ToString("N"),
                FunctionSpecificationId = Guid.NewGuid().ToString("N"),
                FunctionName = $"BugFix_{codeUnitAssignment.Name}_{DateTime.UtcNow:HHmmss}",
                CodeUnit = codeUnitAssignment.Name,
                Signature = "Task<bool> FixBuildError()",
                Description = $"Fix build error in {codeUnitAssignment.Name}: {errorMessage}",
                BusinessLogic = "Analyze build error and implement necessary fixes",
                ErrorHandling = errorMessage,
                SecurityConsiderations = "Maintain existing security patterns",
                ComplexityRating = 7, // Build errors are typically complex
                EstimatedMinutes = 45,
                Priority = "Critical",
                TargetLanguage = codeUnitAssignment.TargetLanguage
            };

            _logger.LogInformation("Spawning bug-fix Method Agent for build error in Code Unit {CodeUnitName}", 
                codeUnitAssignment.Name);

            // Spawn Method Agent to fix the build error
            var success = await _messageCoordinator.SendFunctionAssignmentAsync(bugFixAssignment, cancellationToken);
            
            if (success)
            {
                _logger.LogInformation("Successfully spawned bug-fix Method Agent {AssignmentId} for Code Unit {CodeUnitName}", 
                    bugFixAssignment.AssignmentId, codeUnitAssignment.Name);
            }
            else
            {
                _logger.LogError("Failed to spawn bug-fix Method Agent for Code Unit {CodeUnitName}", 
                    codeUnitAssignment.Name);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling Builder error for Code Unit {CodeUnitName}", 
                codeUnitAssignment.Name);
        }
    }

    /// <summary>
    /// Get expected sections for quality assessment
    /// </summary>
    protected override string[] GetExpectedSectionsForAgent()
    {
        return new[] { "orchestration", "spawning", "monitoring", "completion", "error_handling" };
    }

    /// <summary>
    /// Get estimated duration based on function count and agent spawning
    /// </summary>
    public override int GetEstimatedDurationSeconds(AgentExecutionContext context)
    {
        var baseTime = 60; // 1 minute base for orchestration setup
        
        // Add time based on expected function count for spawning
        var estimatedFunctions = Math.Max(1, (context.InputPrompt?.Length ?? 1000) / 500);
        var spawningTime = estimatedFunctions * 10; // 10 seconds per function to spawn agent
        
        return baseTime + spawningTime;
    }
}