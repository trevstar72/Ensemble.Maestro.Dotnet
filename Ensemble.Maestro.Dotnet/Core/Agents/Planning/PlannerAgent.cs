using Microsoft.Extensions.Logging;
using Ensemble.Maestro.Dotnet.Core.Services;

namespace Ensemble.Maestro.Dotnet.Core.Agents.Planning;

/// <summary>
/// Planning agent responsible for creating high-level project plans and task breakdowns
/// </summary>
public class PlannerAgent : BaseAgent
{
    public PlannerAgent(ILogger<BaseAgent> logger, ILLMService llmService) : base(logger, llmService) { }
    
    public override string AgentType => "Planner";
    public override string AgentName => "Project Planner";
    public override string Priority => "High";
    
    protected override async Task<AgentExecutionResult> ExecuteInternalAsync(AgentExecutionContext context, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Planning project execution for {ProjectId}", context.ProjectId);
        
        var systemPrompt = $@"You are a project planning specialist with expertise in software development project management and technical planning.
        
Create a comprehensive project plan for a {context.TargetLanguage ?? "modern"} application targeting {context.DeploymentTarget ?? "cloud"} deployment.
        
Include:
- Project overview with target language and deployment information
- Detailed execution plan with phases (Requirements Analysis, Architecture Planning, Implementation Strategy, Deployment Planning)
- Risk assessment with technical complexity, resource availability, and timeline feasibility
- Specific recommendations and next steps
- Success criteria and milestones
        
Provide a detailed, actionable project plan formatted in clear markdown.";
        
        var result = await ExecuteLLMCall(systemPrompt, context, cancellationToken);
        
        if (!result.Success)
            return result;
        
        // Add planning-specific artifacts
        result.Artifacts = new List<AgentArtifact>
        {
            new AgentArtifact
            {
                Name = "project_plan.md",
                Type = "markdown",
                Content = result.OutputResponse,
                Path = "/planning/project_plan.md",
                Size = result.OutputResponse.Length
            }
        };
        
        return result;
    }
    
    public override int GetEstimatedDurationSeconds(AgentExecutionContext context)
    {
        // Planning agents typically take 2-4 minutes for complex projects
        var baseTime = 120; // 2 minutes base
        var complexity = (context.InputPrompt?.Length ?? 0) / 500;
        return baseTime + (complexity * 30);
    }
}