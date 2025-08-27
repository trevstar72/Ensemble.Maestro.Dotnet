using Microsoft.Extensions.Logging;
using Ensemble.Maestro.Dotnet.Core.Services;

namespace Ensemble.Maestro.Dotnet.Core.Agents.Planning;

/// <summary>
/// Analysis agent responsible for requirements analysis, feasibility studies, and risk assessment
/// </summary>
public class AnalystAgent : BaseAgent
{
    public AnalystAgent(ILogger<BaseAgent> logger, ILLMService llmService) : base(logger, llmService) { }
    
    public override string AgentType => "Analyst";
    public override string AgentName => "Business Analyst";
    public override string Priority => "Medium";
    
    protected override async Task<AgentExecutionResult> ExecuteInternalAsync(AgentExecutionContext context, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Analyzing requirements and feasibility for {ProjectId}", context.ProjectId);
        
        var systemPrompt = $@"You are a senior business analyst specializing in technical requirements analysis and feasibility studies.
        
Analyze the project requirements and provide a comprehensive analysis for a {context.TargetLanguage ?? "modern"} solution targeting {context.DeploymentTarget ?? "cloud"} deployment.
        
Include:
        - Executive summary
        - Functional and non-functional requirements
        - Technical feasibility assessment
        - Resource requirements estimation
        - Risk analysis (technical and business)
        - Recommendations and success metrics
        - Go/No-go recommendation with confidence level
        
Provide specific, actionable insights formatted in clear markdown.";
        
        var result = await ExecuteLLMCall(systemPrompt, context, cancellationToken);
        
        if (!result.Success)
            return result;
        
        // Add analysis-specific artifacts
        result.Artifacts = new List<AgentArtifact>
        {
            new AgentArtifact
            {
                Name = "requirements_analysis.md",
                Type = "markdown",
                Content = result.OutputResponse,
                Path = "/analysis/requirements_analysis.md",
                Size = result.OutputResponse.Length
            },
            new AgentArtifact
            {
                Name = "risk_assessment.json",
                Type = "json",
                Content = GenerateRiskAssessment(context),
                Path = "/analysis/risk_assessment.json",
                Size = 800
            }
        };
        
        return result;
    }
    
    private string GenerateAnalysisOutput(AgentExecutionContext context)
    {
        var language = context.TargetLanguage ?? "To be determined";
        var deployment = context.DeploymentTarget ?? "Flexible";
        
        return $@"# Requirements Analysis Report

## Executive Summary
Comprehensive analysis of project requirements, technical feasibility, and implementation strategy for {language} solution targeting {deployment} deployment.

## Functional Requirements

### Core Features
1. **User Management**: Authentication, authorization, profile management
2. **Data Processing**: CRUD operations, business logic implementation
3. **API Layer**: RESTful services with comprehensive endpoints
4. **User Interface**: Responsive web interface with modern UX

### Non-Functional Requirements
1. **Performance**: Sub-second response times, high throughput
2. **Scalability**: Support for growing user base and data volume
3. **Reliability**: 99.9% uptime with graceful error handling
4. **Security**: Industry-standard security practices and compliance

## Technical Feasibility

### Technology Assessment
- **Language Choice**: {language} - {GetLanguageAssessment(language)}
- **Deployment Target**: {deployment} - {GetDeploymentAssessment(deployment)}
- **Complexity Level**: Medium to High
- **Development Timeline**: 3-6 months estimated

### Resource Requirements
- **Development Team**: 3-5 developers
- **Infrastructure**: Cloud-based with auto-scaling
- **Third-party Services**: Authentication, payment processing, analytics
- **Estimated Budget**: Medium range for full implementation

## Risk Analysis

### Technical Risks
- **Medium Risk**: Technology stack complexity
- **Low Risk**: Performance bottlenecks
- **Low Risk**: Security vulnerabilities
- **Medium Risk**: Integration challenges

### Business Risks
- **Low Risk**: Market acceptance
- **Medium Risk**: Timeline constraints
- **Low Risk**: Budget overruns
- **High Risk**: Changing requirements

## Recommendations

### Immediate Actions
1. Finalize technical stack selection
2. Set up development and CI/CD pipelines  
3. Create detailed user stories and acceptance criteria
4. Establish testing strategy and quality gates

### Success Metrics
- Code coverage: > 80%
- Performance benchmarks: < 200ms API response
- User satisfaction: > 4.0/5.0 rating
- System availability: > 99.9%

## Conclusion
Project shows strong feasibility with manageable risks. Recommended to proceed with detailed design and prototyping phase.

**Confidence Level**: 85%
**Risk Assessment**: Medium-Low
**Go/No-Go Recommendation**: GO";
    }
    
    private string GenerateRiskAssessment(AgentExecutionContext context)
    {
        return @"{
  ""riskAssessment"": {
    ""technical"": {
      ""complexity"": { ""level"": ""medium"", ""impact"": 6, ""probability"": 0.4 },
      ""performance"": { ""level"": ""low"", ""impact"": 4, ""probability"": 0.2 },
      ""security"": { ""level"": ""low"", ""impact"": 8, ""probability"": 0.1 },
      ""integration"": { ""level"": ""medium"", ""impact"": 5, ""probability"": 0.3 }
    },
    ""business"": {
      ""timeline"": { ""level"": ""medium"", ""impact"": 7, ""probability"": 0.3 },
      ""budget"": { ""level"": ""low"", ""impact"": 6, ""probability"": 0.2 },
      ""requirements"": { ""level"": ""high"", ""impact"": 9, ""probability"": 0.5 }
    },
    ""overallRiskScore"": 4.2,
    ""recommendation"": ""proceed_with_monitoring""
  }
}";
    }
    
    private string GetLanguageAssessment(string? language)
    {
        return language?.ToLower() switch
        {
            "c#" => "Excellent choice for enterprise applications with strong ecosystem",
            "typescript" => "Great for modern web development with type safety",
            "python" => "Perfect for rapid prototyping and data processing",
            "java" => "Solid enterprise choice with mature frameworks",
            "go" => "Excellent for high-performance backend services",
            "rust" => "Outstanding for system-level performance requirements",
            _ => "Technology selection pending detailed evaluation"
        };
    }
    
    private string GetDeploymentAssessment(string? deployment)
    {
        return deployment?.ToLower() switch
        {
            "azure" => "Comprehensive cloud platform with excellent .NET integration",
            "aws" => "Industry-leading cloud services with broad technology support",
            "gcp" => "Strong for data analytics and modern containerized workloads",
            "docker" => "Excellent for development and portable deployments",
            "kubernetes" => "Perfect for scalable, cloud-native applications",
            "on-premises" => "Full control but requires more infrastructure management",
            _ => "Deployment strategy to be determined based on requirements"
        };
    }
    
    public override int GetEstimatedDurationSeconds(AgentExecutionContext context)
    {
        // Analysis work typically takes 2-4 minutes
        var baseTime = 150; // 2.5 minutes base
        var complexity = (context.InputPrompt?.Length ?? 0) / 400;
        return baseTime + (complexity * 25);
    }
}