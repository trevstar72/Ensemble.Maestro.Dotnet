using Microsoft.Extensions.Logging;
using Ensemble.Maestro.Dotnet.Core.Services;

namespace Ensemble.Maestro.Dotnet.Core.Agents.Validating;

public class TesterAgent : BaseAgent
{
    public TesterAgent(ILogger<BaseAgent> logger, ILLMService llmService) : base(logger, llmService) { }
    
    public override string AgentType => "Tester";
    public override string AgentName => "Test Executor";
    public override string Priority => "High";
    
    protected override async Task<AgentExecutionResult> ExecuteInternalAsync(AgentExecutionContext context, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Executing tests for {ProjectId}", context.ProjectId);
        
        var systemPrompt = $@"You are a test execution specialist responsible for comprehensive testing strategies and quality assurance.

Your role is to create detailed test execution plans and assessment frameworks for software systems.

Target Language: {context.TargetLanguage ?? "Multi-language"}
Deployment Target: {context.DeploymentTarget ?? "Multi-platform"}

Include in your response:
• Complete test execution strategy and methodology
• Unit testing approaches and frameworks
• Integration testing patterns and scenarios
• API testing and contract validation
• Performance testing and load scenarios
• Security testing and penetration strategies
• End-to-end testing workflows
• Test automation and CI/CD integration
• Code coverage analysis and reporting
• Test data management and mocking strategies

Provide specific, actionable testing strategies formatted in clear markdown with detailed implementation examples.";
        
        var result = await ExecuteLLMCall(systemPrompt, context, cancellationToken);
        if (!result.Success) return result;
        
        // Add tester specific artifacts
        result.Artifacts = new List<AgentArtifact>
        {
            new AgentArtifact
            {
                Name = "test_execution_report.md",
                Type = "markdown",
                Content = result.OutputResponse,
                Path = "/testing/execution_report.md",
                Size = result.OutputResponse.Length
            },
            new AgentArtifact
            {
                Name = "test_plan.json",
                Type = "json",
                Content = GenerateTestPlan(context),
                Path = "/testing/test_plan.json",
                Size = 2500
            },
            new AgentArtifact
            {
                Name = "test_config.yaml",
                Type = "yaml",
                Content = GenerateTestConfig(context),
                Path = "/testing/test_config.yaml",
                Size = 1500
            }
        };
        
        return result;
    }
    
    private string GenerateTestPlan(AgentExecutionContext context)
    {
        return $@"{{
  ""test_plan"": {{
    ""target_language"": ""{context.TargetLanguage ?? "multi-language"}"",
    ""deployment_target"": ""{context.DeploymentTarget ?? "multi-platform"}"",
    
    ""test_categories"": {{
      ""unit_tests"": {{
        ""framework"": ""xUnit/NUnit/MSTest"",
        ""coverage_target"": 85,
        ""test_types"": [""business_logic"", ""data_validation"", ""edge_cases""]
      }},
      ""integration_tests"": {{
        ""framework"": ""TestContainers"",
        ""coverage_target"": 75,
        ""test_types"": [""api_integration"", ""database_integration"", ""external_services""]
      }},
      ""api_tests"": {{
        ""framework"": ""RestSharp/HttpClient"",
        ""coverage_target"": 90,
        ""test_types"": [""contract_validation"", ""error_handling"", ""authentication""]
      }},
      ""performance_tests"": {{
        ""framework"": ""NBomber/k6"",
        ""metrics"": [""response_time"", ""throughput"", ""concurrent_users""]
,
        ""thresholds"": {{ ""response_time_p95"": ""500ms"", ""error_rate"": ""1%"" }}
      }}
    }},
    
    ""automation"": {{
      ""ci_integration"": true,
      ""parallel_execution"": true,
      ""test_reporting"": ""detailed"",
      ""retry_strategy"": {{ ""max_retries"": 3, ""backoff"": ""exponential"" }}
    }}
  }}
}}";
    }
    
    private string GenerateTestConfig(AgentExecutionContext context)
    {
        return $@"test_configuration:
  target: {context.TargetLanguage ?? "multi-language"}
  
  frameworks:
    unit_testing:
      primary: xUnit
      mocking: Moq
      assertions: FluentAssertions
      
    integration_testing:
      containers: TestContainers
      database: InMemory/Docker
      web_testing: WebApplicationFactory
      
  execution:
    parallel: true
    max_threads: 4
    timeout: 300s
    retry_failed: true
    
  reporting:
    formats: [trx, html, junit]
    coverage_tool: coverlet
    output_directory: ./TestResults
    
  coverage_targets:
    line_coverage: 85
    branch_coverage: 80
    method_coverage: 90";
    }
}