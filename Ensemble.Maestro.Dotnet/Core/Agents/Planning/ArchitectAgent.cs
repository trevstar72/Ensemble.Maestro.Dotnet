using Microsoft.Extensions.Logging;
using Ensemble.Maestro.Dotnet.Core.Services;

namespace Ensemble.Maestro.Dotnet.Core.Agents.Planning;

/// <summary>
/// Architecture agent responsible for designing system architecture and technical specifications
/// </summary>
public class ArchitectAgent : BaseAgent
{
    public ArchitectAgent(ILogger<BaseAgent> logger, ILLMService llmService) : base(logger, llmService) { }
    
    public override string AgentType => "Architect";
    public override string AgentName => "System Architect";
    public override string Priority => "High";
    
    protected override async Task<AgentExecutionResult> ExecuteInternalAsync(AgentExecutionContext context, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Designing system architecture for {ProjectId}", context.ProjectId);
        
        var systemPrompt = $@"You are a senior system architect with expertise in scalable system design.
        
Create a comprehensive system architecture specification for a {context.TargetLanguage ?? "modern"} application targeting {context.DeploymentTarget ?? "cloud"} deployment.
        
Include:
        - Architecture overview
        - Core components and layers
        - Design patterns and principles
        - Scalability considerations
        - Security architecture
        - Performance targets
        - Technology stack recommendations
        
Provide specific, actionable technical recommendations formatted in clear markdown.";
        
        var result = await ExecuteLLMCall(systemPrompt, context, cancellationToken);
        
        if (!result.Success)
            return result;
        
        // Add architecture-specific artifacts
        result.Artifacts = new List<AgentArtifact>
        {
            new AgentArtifact
            {
                Name = "system_architecture.md",
                Type = "markdown",
                Content = result.OutputResponse,
                Path = "/architecture/system_architecture.md",
                Size = result.OutputResponse.Length
            },
            new AgentArtifact
            {
                Name = "component_diagram.mermaid",
                Type = "mermaid",
                Content = GenerateComponentDiagram(context),
                Path = "/architecture/component_diagram.mermaid",
                Size = 500
            }
        };
        
        return result;
    }
    
    private string GenerateArchitectureOutput(AgentExecutionContext context)
    {
        var language = context.TargetLanguage ?? "Multi-language";
        var deployment = context.DeploymentTarget ?? "Cloud-native";
        
        return $@"# System Architecture Specification

## Architecture Overview
Designing a {language} solution targeting {deployment} deployment with scalable, maintainable architecture.

## Core Components

### 1. Application Layer
- **Framework**: {GetRecommendedFramework(language)}
- **Pattern**: Clean Architecture with CQRS
- **Communication**: RESTful APIs with WebSocket support

### 2. Data Layer  
- **Database**: {GetRecommendedDatabase(deployment)}
- **Caching**: Redis for session and query caching
- **Storage**: Blob storage for files and artifacts

### 3. Infrastructure Layer
- **Deployment**: {deployment} containerized deployment
- **Monitoring**: Application Performance Monitoring (APM)
- **Logging**: Centralized logging with structured format

## Design Patterns
- **Repository Pattern**: For data access abstraction
- **Factory Pattern**: For agent instantiation
- **Observer Pattern**: For real-time updates
- **Strategy Pattern**: For configurable algorithms

## Scalability Considerations
- Horizontal scaling with load balancing
- Asynchronous processing for long-running tasks
- Database sharding for high-volume data
- CDN integration for static assets

## Security Architecture
- JWT-based authentication
- Role-based access control (RBAC)
- API rate limiting and throttling
- Data encryption at rest and in transit

## Performance Targets
- API response time: < 200ms (95th percentile)
- Database query time: < 50ms average
- System uptime: 99.9% SLA
- Concurrent users: 10,000+

## Technology Stack
- **Backend**: {language}
- **Database**: PostgreSQL/SQL Server
- **Cache**: Redis
- **Message Queue**: RabbitMQ/Azure Service Bus
- **Deployment**: Docker + Kubernetes/{deployment}

Architecture ready for detailed design phase.";
    }
    
    private string GenerateComponentDiagram(AgentExecutionContext context)
    {
        return @"graph TB
    A[Web Client] --> B[API Gateway]
    B --> C[Authentication Service]
    B --> D[Application Services]
    D --> E[Business Logic Layer]
    E --> F[Data Access Layer]
    F --> G[(Database)]
    E --> H[Cache Layer]
    H --> I[(Redis)]
    D --> J[Message Queue]
    J --> K[Background Services]";
    }
    
    private string GetRecommendedFramework(string? language)
    {
        return language?.ToLower() switch
        {
            "c#" => "ASP.NET Core",
            "typescript" => "Node.js with Express/Fastify",
            "python" => "FastAPI or Django",
            "java" => "Spring Boot",
            "go" => "Gin or Fiber",
            "rust" => "Actix Web or Axum",
            _ => "Modern web framework"
        };
    }
    
    private string GetRecommendedDatabase(string? deployment)
    {
        return deployment?.ToLower() switch
        {
            "azure" => "Azure SQL Database",
            "aws" => "Amazon RDS PostgreSQL",
            "gcp" => "Cloud SQL PostgreSQL",
            "docker" => "PostgreSQL container",
            "kubernetes" => "PostgreSQL StatefulSet",
            _ => "PostgreSQL"
        };
    }
    
    public override int GetEstimatedDurationSeconds(AgentExecutionContext context)
    {
        // Architecture work typically takes 3-5 minutes
        var baseTime = 180; // 3 minutes base
        var complexity = (context.InputPrompt?.Length ?? 0) / 300;
        return baseTime + (complexity * 20);
    }
}