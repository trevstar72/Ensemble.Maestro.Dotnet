using Ensemble.Maestro.Dotnet.Core.Agents;
using Ensemble.Maestro.Dotnet.Core.Data.Entities;
using Ensemble.Maestro.Dotnet.Core.Data.Repositories;
using Microsoft.Extensions.Logging;

namespace Ensemble.Maestro.Dotnet.Core.Services;

/// <summary>
/// Service for executing agents and managing their lifecycle
/// </summary>
public class AgentExecutionService
{
    private readonly IAgentFactory _agentFactory;
    private readonly IAgentExecutionRepository _agentExecutionRepository;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<AgentExecutionService> _logger;
    
    public AgentExecutionService(
        IAgentFactory agentFactory,
        IAgentExecutionRepository agentExecutionRepository,
        IServiceScopeFactory serviceScopeFactory,
        ILogger<AgentExecutionService> logger)
    {
        _agentFactory = agentFactory;
        _agentExecutionRepository = agentExecutionRepository;
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
    }
    
    /// <summary>
    /// Execute an agent with the given context
    /// </summary>
    public async Task<AgentExecution> ExecuteAgentAsync(
        Guid projectId,
        Guid pipelineExecutionId,
        Guid stageExecutionId,
        string agentType,
        string inputPrompt,
        AgentExecutionContext? additionalContext = null,
        CancellationToken cancellationToken = default)
    {
        // Create agent execution record
        var agentExecution = new AgentExecution
        {
            ProjectId = projectId,
            PipelineExecutionId = pipelineExecutionId,
            StageExecutionId = stageExecutionId,
            AgentType = agentType,
            AgentName = $"{agentType}Agent",
            Status = "Running",
            InputPrompt = inputPrompt,
            Priority = "Medium",
            ModelUsed = "gpt-4o",
            Temperature = 0.7f,
            MaxTokens = 4000
        };
        
        await _agentExecutionRepository.AddAsync(agentExecution);
        await _agentExecutionRepository.SaveChangesAsync();
        
        try
        {
            // Create agent instance
            _logger.LogInformation("Creating agent instance of type: {AgentType}", agentType);
            var agent = _agentFactory.CreateAgent(agentType);
            if (agent == null)
            {
                _logger.LogError("AgentFactory returned null for agent type: {AgentType}", agentType);
                throw new InvalidOperationException($"Unable to create agent of type: {agentType}");
            }
            _logger.LogInformation("Agent {AgentType} created successfully", agentType);
            
            // Create execution context
            var context = additionalContext ?? new AgentExecutionContext();
            context.ExecutionId = agentExecution.Id;
            context.ProjectId = projectId;
            context.PipelineExecutionId = pipelineExecutionId;
            context.StageExecutionId = stageExecutionId;
            context.InputPrompt = inputPrompt;
            context.ModelUsed = agentExecution.ModelUsed;
            context.Temperature = agentExecution.Temperature ?? 0.7f;
            context.MaxTokens = agentExecution.MaxTokens ?? 4000;
            
            _logger.LogInformation("Executing agent {AgentType} with context: ProjectId={ProjectId}, InputPrompt='{InputPrompt}'", 
                agentType, context.ProjectId, context.InputPrompt.Substring(0, Math.Min(100, context.InputPrompt.Length)) + "...");
            
            // Execute agent
            var result = await agent.ExecuteAsync(context, cancellationToken);
            
            _logger.LogInformation("Agent {AgentType} execution result: Success={Success}, Duration={Duration}s, Tokens={InputTokens}+{OutputTokens}, Cost=${Cost}", 
                agentType, result.Success, result.DurationSeconds, result.InputTokens, result.OutputTokens, result.ExecutionCost);
            
            // Update execution record with results
            agentExecution.Status = result.Success ? "Completed" : "Failed";
            agentExecution.CompletedAt = DateTime.UtcNow;
            agentExecution.DurationSeconds = result.DurationSeconds;
            agentExecution.OutputResponse = result.OutputResponse;
            agentExecution.ErrorMessage = result.ErrorMessage;
            agentExecution.InputTokens = result.InputTokens;
            agentExecution.OutputTokens = result.OutputTokens;
            agentExecution.TotalTokens = result.TotalTokens;
            agentExecution.ExecutionCost = result.ExecutionCost;
            agentExecution.QualityScore = result.QualityScore;
            agentExecution.ConfidenceScore = result.ConfidenceScore;
            
            // Save agent messages
            foreach (var message in result.Messages)
            {
                agentExecution.Messages.Add(new Core.Data.Entities.AgentMessage
                {
                    Role = message.Role,
                    Content = message.Content,
                    CreatedAt = message.Timestamp
                });
            }
            
            await _agentExecutionRepository.UpdateAsync(agentExecution);
            await _agentExecutionRepository.SaveChangesAsync();
            
            // Update pipeline function counts
            await UpdatePipelineFunctionCounts(pipelineExecutionId, result.Success);
            
            _logger.LogInformation("Agent execution completed: {AgentType} for {ExecutionId} with status {Status}",
                agentType, agentExecution.Id, agentExecution.Status);
            
            return agentExecution;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Agent execution failed: {AgentType} for {ExecutionId}", agentType, agentExecution.Id);
            
            // Update execution record with error
            agentExecution.Status = "Failed";
            agentExecution.CompletedAt = DateTime.UtcNow;
            agentExecution.DurationSeconds = (int)(DateTime.UtcNow - agentExecution.StartedAt).TotalSeconds;
            agentExecution.ErrorMessage = ex.Message;
            
            await _agentExecutionRepository.UpdateAsync(agentExecution);
            await _agentExecutionRepository.SaveChangesAsync();
            
            // Update pipeline function counts for failed execution
            await UpdatePipelineFunctionCounts(pipelineExecutionId, false);
            
            return agentExecution;
        }
    }
    
    /// <summary>
    /// Execute all agents for a specific stage
    /// </summary>
    public async Task<List<AgentExecution>> ExecuteStageAgentsAsync(
        Guid projectId,
        Guid pipelineExecutionId,
        Guid stageExecutionId,
        string stageName,
        string inputPrompt,
        AgentExecutionContext? baseContext = null,
        CancellationToken cancellationToken = default)
    {
        var agentTypes = AgentFactory.GetAgentTypesForStage(stageName);
        var executions = new List<AgentExecution>();
        
        _logger.LogInformation("=== AgentExecutionService: Executing {AgentCount} agents for stage {StageName} ===", 
            agentTypes.Length, stageName);
        _logger.LogInformation("Agent types: [{AgentTypes}]", string.Join(", ", agentTypes));
        _logger.LogInformation("ProjectId: {ProjectId}, PipelineId: {PipelineId}, StageId: {StageId}", 
            projectId, pipelineExecutionId, stageExecutionId);
        
        foreach (var agentType in agentTypes)
        {
            _logger.LogInformation("Attempting to execute agent: {AgentType}", agentType);
            
            try
            {
                var execution = await ExecuteAgentAsync(
                    projectId, 
                    pipelineExecutionId, 
                    stageExecutionId, 
                    agentType, 
                    inputPrompt, 
                    baseContext, 
                    cancellationToken);
                
                executions.Add(execution);
                _logger.LogInformation("Agent {AgentType} execution completed with status: {Status}", 
                    agentType, execution.Status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to execute agent {AgentType} for stage {StageName}: {ErrorMessage}", 
                    agentType, stageName, ex.Message);
            }
        }
        
        _logger.LogInformation("=== AgentExecutionService: Completed stage {StageName} with {CompletedCount}/{TotalCount} successful executions ===", 
            stageName, executions.Count, agentTypes.Length);
        
        return executions;
    }
    
    /// <summary>
    /// Update pipeline function counts when an agent execution completes
    /// </summary>
    private async Task UpdatePipelineFunctionCounts(Guid pipelineExecutionId, bool success)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var pipelineRepository = scope.ServiceProvider.GetRequiredService<IPipelineExecutionRepository>();
        
        var pipelineResult = await pipelineRepository.GetByIdAsync(pipelineExecutionId);
        if (!pipelineResult.IsSuccess || pipelineResult.Value == null) return;
        
        var pipeline = pipelineResult.Value;
        
        if (success)
        {
            pipeline.CompletedFunctions++;
        }
        else
        {
            pipeline.FailedFunctions++;
        }
        
        // Update progress percentage based on completed functions
        if (pipeline.TotalFunctions.HasValue && pipeline.TotalFunctions.Value > 0)
        {
            var totalCompleted = pipeline.CompletedFunctions + pipeline.FailedFunctions;
            pipeline.ProgressPercentage = Math.Min(100, (int)((double)totalCompleted / pipeline.TotalFunctions.Value * 100));
        }
        
        await pipelineRepository.UpdateAsync(pipeline);
        await pipelineRepository.SaveChangesAsync();
    }
}