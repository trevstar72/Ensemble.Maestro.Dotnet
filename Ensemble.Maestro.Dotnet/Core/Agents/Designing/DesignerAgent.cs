using Microsoft.Extensions.Logging;
using Ensemble.Maestro.Dotnet.Core.Services;

namespace Ensemble.Maestro.Dotnet.Core.Agents.Designing;

/// <summary>
/// Designer agent responsible for creating detailed system designs and component specifications
/// </summary>
public class DesignerAgent : BaseDesignerAgent
{
    public DesignerAgent(
        ILogger<BaseAgent> logger, 
        ILLMService llmService, 
        IDesignerOutputStorageService designerOutputStorageService) 
        : base(logger, llmService, designerOutputStorageService) { }
    
    public override string AgentType => "Designer";
    public override string AgentName => "System Designer";
    public override string Priority => "High";
    
    protected override async Task<AgentExecutionResult> ExecuteInternalAsync(AgentExecutionContext context, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating detailed system design for {ProjectId}", context.ProjectId);
        
        var systemPrompt = $@"You are an expert system designer with deep knowledge of software architecture patterns and modern development practices.

Create a comprehensive detailed system design for a {context.TargetLanguage ?? "modern"} application targeting {context.DeploymentTarget ?? "cloud"} deployment.

Include:
- Component design for all layers (presentation, application, business, data)
- Database schema and relationships
- RESTful API design with endpoints
- Security design with authentication/authorization
- Performance design with caching strategies
- Scalability design for horizontal/vertical scaling

Provide specific, implementable technical specifications formatted in clear markdown.";
        
        var result = await ExecuteLLMCall(systemPrompt, context, cancellationToken);
        
        if (!result.Success)
            return result;
        
        // Add design-specific artifacts
        result.Artifacts = new List<AgentArtifact>
        {
            new AgentArtifact
            {
                Name = "system_design.md",
                Type = "markdown",
                Content = result.OutputResponse,
                Path = "/design/system_design.md",
                Size = result.OutputResponse.Length
            },
            new AgentArtifact
            {
                Name = "data_model.json",
                Type = "json",
                Content = GenerateDataModel(context),
                Path = "/design/data_model.json",
                Size = 1200
            }
        };
        
        return result;
    }
    
    private string GenerateDataModel(AgentExecutionContext context)
    {
        return @"{
  ""entities"": {
    ""User"": {
      ""properties"": {
        ""id"": ""Guid"",
        ""username"": ""string"",
        ""email"": ""string"",
        ""passwordHash"": ""string"",
        ""createdAt"": ""DateTime"",
        ""lastLoginAt"": ""DateTime?""
      },
      ""relationships"": {
        ""projects"": ""Project[]"",
        ""executions"": ""PipelineExecution[]""
      }
    },
    ""Project"": {
      ""properties"": {
        ""id"": ""Guid"",
        ""name"": ""string"",
        ""description"": ""string"",
        ""targetLanguage"": ""string?"",
        ""deploymentTarget"": ""string?"",
        ""status"": ""string"",
        ""createdAt"": ""DateTime"",
        ""updatedAt"": ""DateTime""
      },
      ""relationships"": {
        ""owner"": ""User"",
        ""executions"": ""PipelineExecution[]""
      }
    }
  }
}";
    }
    
    public override int GetEstimatedDurationSeconds(AgentExecutionContext context)
    {
        // Design work typically takes 3-5 minutes for comprehensive designs
        var baseTime = 200; // 3.3 minutes base
        var complexity = (context.InputPrompt?.Length ?? 0) / 250;
        return baseTime + (complexity * 15);
    }
}