using Microsoft.Extensions.Logging;
using Ensemble.Maestro.Dotnet.Core.Services;

namespace Ensemble.Maestro.Dotnet.Core.Agents;

/// <summary>
/// Base abstract class for all agents providing common functionality
/// </summary>
public abstract class BaseAgent : IAgent
{
    protected readonly ILogger<BaseAgent> _logger;
    protected readonly ILLMService _llmService;
    
    protected BaseAgent(ILogger<BaseAgent> logger, ILLMService llmService)
    {
        _logger = logger;
        _llmService = llmService;
    }
    
    /// <summary>
    /// Agent type identifier
    /// </summary>
    public abstract string AgentType { get; }
    
    /// <summary>
    /// Human-readable agent name
    /// </summary>
    public abstract string AgentName { get; }
    
    /// <summary>
    /// Priority level for execution
    /// </summary>
    public virtual string Priority => "Medium";
    
    /// <summary>
    /// Execute the agent with the given context
    /// </summary>
    public async Task<AgentExecutionResult> ExecuteAsync(AgentExecutionContext context, CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        
        try
        {
            _logger.LogInformation("Starting execution of agent {AgentType} for execution {ExecutionId}", 
                AgentType, context.ExecutionId);
            
            // Validate context before execution
            if (!CanExecute(context))
            {
                var errorMessage = $"Agent {AgentType} cannot execute with the provided context";
                _logger.LogError("VALIDATION FAILURE: {ErrorMessage}. Context details: ProjectId={ProjectId}, InputPrompt='{InputPrompt}', PipelineExecutionId={PipelineExecutionId}",
                    errorMessage, context.ProjectId, context.InputPrompt?.Length > 0 ? "PROVIDED" : "EMPTY", context.PipelineExecutionId);
                return AgentExecutionResult.Failure(errorMessage);
            }
            
            // Perform pre-execution setup
            await PreExecuteAsync(context, cancellationToken);
            
            // Execute the main agent logic
            var result = await ExecuteInternalAsync(context, cancellationToken);
            
            // Perform post-execution cleanup
            await PostExecuteAsync(context, result, cancellationToken);
            
            // Calculate execution time
            result.DurationSeconds = (int)(DateTime.UtcNow - startTime).TotalSeconds;
            
            _logger.LogInformation("Completed execution of agent {AgentType} in {DurationSeconds}s with success: {Success}", 
                AgentType, result.DurationSeconds, result.Success);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing agent {AgentType} for execution {ExecutionId}", 
                AgentType, context.ExecutionId);
            
            return AgentExecutionResult.Failure($"Agent execution failed: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Validate if the agent can execute with the given context
    /// </summary>
    public virtual bool CanExecute(AgentExecutionContext context)
    {
        return !string.IsNullOrEmpty(context.InputPrompt) && 
               context.ProjectId != Guid.Empty && 
               context.PipelineExecutionId != Guid.Empty;
    }
    
    /// <summary>
    /// Get estimated duration for execution in seconds
    /// </summary>
    public virtual int GetEstimatedDurationSeconds(AgentExecutionContext context)
    {
        // Default estimation based on input complexity
        var baseTime = 30; // 30 seconds base
        var inputLength = context.InputPrompt?.Length ?? 0;
        var complexityFactor = Math.Min(inputLength / 1000, 10); // Max 10x multiplier
        
        return baseTime + (complexityFactor * 15);
    }
    
    /// <summary>
    /// Pre-execution setup hook
    /// </summary>
    protected virtual Task PreExecuteAsync(AgentExecutionContext context, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
    
    /// <summary>
    /// Main execution logic - must be implemented by derived classes
    /// </summary>
    protected abstract Task<AgentExecutionResult> ExecuteInternalAsync(AgentExecutionContext context, CancellationToken cancellationToken);
    
    /// <summary>
    /// Post-execution cleanup hook
    /// </summary>
    protected virtual Task PostExecuteAsync(AgentExecutionContext context, AgentExecutionResult result, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
    
    /// <summary>
    /// Calculate execution cost based on token usage
    /// </summary>
    protected virtual decimal CalculateExecutionCost(int inputTokens, int outputTokens, string model = "gpt-4o")
    {
        // Cost per 1K tokens for different models
        var costs = new Dictionary<string, (decimal input, decimal output)>
        {
            { "gpt-4o", (0.0025m, 0.01m) },
            { "gpt-4", (0.03m, 0.06m) },
            { "gpt-3.5-turbo", (0.001m, 0.002m) }
        };
        
        if (!costs.ContainsKey(model))
            model = "gpt-4o";
        
        var (inputCost, outputCost) = costs[model];
        
        return (inputTokens / 1000m * inputCost) + (outputTokens / 1000m * outputCost);
    }
    
    /// <summary>
    /// Generate quality and confidence scores based on actual content analysis
    /// </summary>
    protected virtual (int quality, int confidence) AnalyzeOutputQuality(string output, AgentExecutionContext context)
    {
        if (string.IsNullOrEmpty(output))
            return (0, 0);
        
        // Real quality assessment based on content characteristics
        var quality = 70; // Base quality score
        var confidence = 60; // Base confidence score
        
        // Length-based scoring (more comprehensive outputs tend to be higher quality)
        if (output.Length > 2000) quality += 15;
        else if (output.Length > 1000) quality += 10;
        else if (output.Length > 500) quality += 5;
        
        // Structure-based scoring (proper markdown structure indicates quality)
        if (output.Contains("##")) quality += 10;
        if (output.Contains("- ")) quality += 5;
        if (output.Contains("```")) quality += 5;
        
        // Completeness scoring (sections expected for agent type)
        var agentSpecificSections = GetExpectedSectionsForAgent();
        var foundSections = agentSpecificSections.Count(section => 
            output.Contains(section, StringComparison.OrdinalIgnoreCase));
        quality += (foundSections * 10 / agentSpecificSections.Length) * 10;
        
        // Confidence based on prompt match and context alignment
        if (context.InputPrompt != null && output.Length > context.InputPrompt.Length)
            confidence += 20;
        
        if (!string.IsNullOrEmpty(context.TargetLanguage) && 
            output.Contains(context.TargetLanguage, StringComparison.OrdinalIgnoreCase))
            confidence += 15;
        
        return (Math.Min(100, quality), Math.Min(100, confidence));
    }
    
    /// <summary>
    /// Get expected sections for this agent type for quality assessment
    /// </summary>
    protected virtual string[] GetExpectedSectionsForAgent()
    {
        return AgentType switch
        {
            "Planner" => new[] { "overview", "plan", "phase", "recommendation" },
            "Architect" => new[] { "architecture", "component", "design", "pattern" },
            "Analyst" => new[] { "analysis", "requirement", "risk", "feasibility" },
            "Designer" => new[] { "design", "specification", "component", "pattern" },
            "UIDesigner" => new[] { "ui", "design", "component", "style" },
            "APIDesigner" => new[] { "api", "endpoint", "schema", "documentation" },
            "Coordinator" => new[] { "coordination", "strategy", "distribution", "execution" },
            "Builder" => new[] { "build", "artifact", "compilation", "deployment" },
            "Validator" => new[] { "validation", "quality", "compliance", "result" },
            "Tester" => new[] { "test", "coverage", "result", "execution" },
            _ => new[] { "overview", "result", "analysis" }
        };
    }
    
    /// <summary>
    /// Execute LLM call and return structured result
    /// </summary>
    protected async Task<AgentExecutionResult> ExecuteLLMCall(
        string systemPrompt,
        AgentExecutionContext context,
        CancellationToken cancellationToken)
    {
        var llmResponse = await _llmService.GenerateResponseAsync(
            systemPrompt,
            context.InputPrompt,
            context.MaxTokens,
            context.Temperature,
            cancellationToken,
            AgentType,
            context.Stage);
        
        if (!llmResponse.Success)
        {
            return AgentExecutionResult.Failure(llmResponse.ErrorMessage);
        }
        
        // Analyze quality based on actual output
        var (quality, confidence) = AnalyzeOutputQuality(llmResponse.Content, context);
        
        var result = new AgentExecutionResult
        {
            Success = true,
            OutputResponse = llmResponse.Content,
            QualityScore = quality,
            ConfidenceScore = confidence,
            InputTokens = llmResponse.InputTokens,
            OutputTokens = llmResponse.OutputTokens,
            ExecutionCost = llmResponse.Cost,
            DurationSeconds = (int)llmResponse.Duration.TotalSeconds,
            Messages = new List<AgentMessage>
            {
                new AgentMessage { Role = "system", Content = systemPrompt },
                new AgentMessage { Role = "user", Content = context.InputPrompt },
                new AgentMessage { Role = "assistant", Content = llmResponse.Content }
            }
        };
        
        return result;
    }
}