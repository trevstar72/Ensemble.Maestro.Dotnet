using Microsoft.Extensions.Logging;
using Ensemble.Maestro.Dotnet.Core.Services;

namespace Ensemble.Maestro.Dotnet.Core.Agents.Swarming;

/// <summary>
/// Distributor agent responsible for task distribution and workload balancing across agent swarms
/// </summary>
public class DistributorAgent : BaseAgent
{
    public DistributorAgent(ILogger<BaseAgent> logger, ILLMService llmService) : base(logger, llmService) { }
    
    public override string AgentType => "Distributor";
    public override string AgentName => "Task Distributor";
    public override string Priority => "Medium";
    
    protected override async Task<AgentExecutionResult> ExecuteInternalAsync(AgentExecutionContext context, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Distributing tasks across agent swarm for {ProjectId}", context.ProjectId);
        
        var systemPrompt = $@"You are a task distribution specialist responsible for workload balancing across agent swarms.

Your role is to create intelligent task distribution strategies for optimal agent utilization.

Agent Pool Size: {context.AgentPoolSize ?? 5} concurrent agents
Target Language: {context.TargetLanguage ?? "Multi-language"}
Deployment Target: {context.DeploymentTarget ?? "Distributed"}

Include in your response:
• Load balancing algorithms and strategies
• Task assignment logic based on agent capabilities
• Work queue management and prioritization
• Dynamic rebalancing mechanisms
• Performance optimization techniques
• Resource utilization strategies
• Agent pool allocation recommendations
• Bottleneck detection and resolution
• Work stealing patterns for idle agents

Provide specific, actionable distribution strategies formatted in clear markdown with implementation details.";
        
        var result = await ExecuteLLMCall(systemPrompt, context, cancellationToken);
        if (!result.Success) return result;
        
        // Add distributor specific artifacts
        result.Artifacts = new List<AgentArtifact>
        {
            new AgentArtifact
            {
                Name = "task_distribution_strategy.md",
                Type = "markdown",
                Content = result.OutputResponse,
                Path = "/swarming/distribution_strategy.md",
                Size = result.OutputResponse.Length
            }
        };
        
        return result;
    }
    
    private string GenerateDistributionOutput(AgentExecutionContext context)
    {
        var agentPoolSize = context.AgentPoolSize ?? 5;
        
        return $@"# Task Distribution Strategy

## Distribution Overview
Implementing intelligent task distribution across {agentPoolSize} agents with dynamic load balancing and capability-based routing.

## Distribution Algorithms

### Load Balancing Strategy
- **Algorithm**: Weighted Round Robin with health checks
- **Weight Calculation**: Based on agent performance history
- **Health Monitoring**: Real-time agent availability assessment
- **Failover**: Automatic redistribution on agent failure

### Task Assignment Logic
1. **Capability Matching**: Match task requirements to agent capabilities
2. **Load Assessment**: Check current agent workload
3. **Priority Handling**: Higher priority tasks get immediate assignment
4. **Resource Optimization**: Consider CPU, memory, and network usage

## Work Distribution Pattern

### Task Queue Management
- **High Priority Queue**: Critical tasks with immediate processing
- **Standard Queue**: Regular development tasks
- **Background Queue**: Non-urgent optimization tasks
- **Retry Queue**: Failed tasks awaiting retry

### Agent Pool Allocation
- **Planning Agents**: 25% of pool ({Math.Max(1, agentPoolSize / 4)} agents)
- **Design Agents**: 30% of pool ({Math.Max(1, (int)(agentPoolSize * 0.3))} agents)  
- **Build Agents**: 25% of pool ({Math.Max(1, agentPoolSize / 4)} agents)
- **Validation Agents**: 20% of pool ({Math.Max(1, agentPoolSize / 5)} agents)

## Performance Optimization

### Dynamic Rebalancing
- Monitor agent performance in real-time
- Redistribute workload when bottlenecks detected
- Scale agent allocation based on stage requirements
- Implement work stealing for idle agents

Task distribution strategy optimized for maximum throughput and reliability.";
    }
    
    public override int GetEstimatedDurationSeconds(AgentExecutionContext context)
    {
        return 90 + ((context.AgentPoolSize ?? 5) * 8);
    }
}