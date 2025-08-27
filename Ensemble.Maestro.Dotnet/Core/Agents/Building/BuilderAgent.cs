using Microsoft.Extensions.Logging;
using Ensemble.Maestro.Dotnet.Core.Services;

namespace Ensemble.Maestro.Dotnet.Core.Agents.Building;

public class BuilderAgent : BaseAgent
{
    public BuilderAgent(ILogger<BaseAgent> logger, ILLMService llmService) : base(logger, llmService) { }
    
    public override string AgentType => "Builder";
    public override string AgentName => "System Builder";
    public override string Priority => "High";
    
    protected override async Task<AgentExecutionResult> ExecuteInternalAsync(AgentExecutionContext context, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Executing build pipeline for {ProjectId}", context.ProjectId);
        
        var systemPrompt = $@"You are a system builder specialist responsible for orchestrating and executing comprehensive build pipelines.

Your role is to create detailed build execution plans and strategies based on project requirements.

Target Language: {context.TargetLanguage ?? "Multi-language"}
Deployment Target: {context.DeploymentTarget ?? "Multi-platform"}

Include in your response:
• Complete build pipeline design and execution strategy
• Dependency management and resolution
• Artifact generation and packaging
• Build optimization techniques
• Environment configuration and setup
• Error handling and recovery procedures
• Performance metrics and monitoring
• Deployment package preparation
• Build validation and testing integration
• CI/CD pipeline integration

Provide specific, actionable build strategies formatted in clear markdown with detailed implementation steps.";
        
        var result = await ExecuteLLMCall(systemPrompt, context, cancellationToken);
        if (!result.Success) return result;
        
        // Add builder specific artifacts
        result.Artifacts = new List<AgentArtifact>
        {
            new AgentArtifact
            {
                Name = "build_execution_report.md",
                Type = "markdown",
                Content = result.OutputResponse,
                Path = "/build/execution_report.md",
                Size = result.OutputResponse.Length
            },
            new AgentArtifact
            {
                Name = "build_config.yaml",
                Type = "yaml",
                Content = GenerateBuildConfig(context),
                Path = "/build/build_config.yaml",
                Size = 1200
            }
        };
        
        return result;
    }
    
    private string GenerateBuildConfig(AgentExecutionContext context)
    {
        return $@"build:
  target_language: {context.TargetLanguage ?? "multi-language"}
  deployment_target: {context.DeploymentTarget ?? "multi-platform"}
  
  stages:
    - name: dependencies
      commands:
        - restore packages
        - verify dependencies
      timeout: 300
      
    - name: compile
      commands:
        - build source
        - generate artifacts
      timeout: 600
      
    - name: package
      commands:
        - create deployment package
        - prepare configuration
      timeout: 180
      
  artifacts:
    - binaries/*
    - config/*
    - docs/*
    
  optimization:
    parallel_build: true
    cache_dependencies: true
    incremental_build: true";
    }
}