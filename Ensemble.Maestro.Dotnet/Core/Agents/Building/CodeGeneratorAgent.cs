using Microsoft.Extensions.Logging;
using Ensemble.Maestro.Dotnet.Core.Services;

namespace Ensemble.Maestro.Dotnet.Core.Agents.Building;

public class CodeGeneratorAgent : BaseAgent
{
    public CodeGeneratorAgent(ILogger<BaseAgent> logger, ILLMService llmService) : base(logger, llmService) { }
    
    public override string AgentType => "CodeGenerator";
    public override string AgentName => "Code Generator";
    public override string Priority => "High";
    
    protected override async Task<AgentExecutionResult> ExecuteInternalAsync(AgentExecutionContext context, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Generating code for {ProjectId}", context.ProjectId);
        
        var systemPrompt = $@"You are a code generation specialist responsible for creating production-ready source code files.

Your role is to generate ACTUAL SOURCE CODE FILES, not documentation or strategies.

Target Language: {context.TargetLanguage ?? "C#"}
Deployment Target: {context.DeploymentTarget ?? "Cloud"}

Generate the following ACTUAL CODE FILES:

1. **Main Application Class** (Program.cs or main.py or app.js)
2. **API Controller** with CRUD operations
3. **Business Service Layer** with core logic
4. **Data Model/Entity** classes
5. **Database Repository** interface and implementation
6. **Configuration** files (appsettings.json, requirements.txt, package.json)
7. **Unit Test** classes with test methods
8. **Docker Configuration** (Dockerfile, docker-compose.yml)

For each file, provide:
- Complete file path (e.g., /src/Controllers/UserController.cs)
- Full source code with proper syntax
- Imports/using statements
- Class definitions, methods, properties
- Error handling and validation
- Comments explaining key functionality

Output format:
```
## [FILE PATH]
```[language]
[COMPLETE SOURCE CODE]
```

Generate REAL, COMPILABLE CODE - not explanations or strategies.";
        
        var result = await ExecuteLLMCall(systemPrompt, context, cancellationToken);
        if (!result.Success) return result;
        
        // Add code generator specific artifacts
        result.Artifacts = new List<AgentArtifact>
        {
            new AgentArtifact
            {
                Name = "code_generation_report.md",
                Type = "markdown",
                Content = result.OutputResponse,
                Path = "/codegen/generation_report.md",
                Size = result.OutputResponse.Length
            },
            new AgentArtifact
            {
                Name = "code_templates.json",
                Type = "json",
                Content = GenerateCodeTemplates(context),
                Path = "/codegen/templates.json",
                Size = 2500
            },
            new AgentArtifact
            {
                Name = "generation_config.yaml",
                Type = "yaml",
                Content = GenerateGenerationConfig(context),
                Path = "/codegen/config.yaml",
                Size = 800
            }
        };
        
        return result;
    }
    
    private string GenerateCodeTemplates(AgentExecutionContext context)
    {
        return $@"{{
  ""templates"": {{
    ""controller"": {{
      ""language"": ""{context.TargetLanguage ?? "csharp"}"",
      ""pattern"": ""MVC Controller"",
      ""features"": [""CRUD operations"", ""validation"", ""error handling""]
    }},
    ""model"": {{
      ""language"": ""{context.TargetLanguage ?? "csharp"}"",
      ""pattern"": ""Entity Framework Model"",
      ""features"": [""data annotations"", ""relationships"", ""validation""]
    }},
    ""service"": {{
      ""language"": ""{context.TargetLanguage ?? "csharp"}"",
      ""pattern"": ""Service Layer"",
      ""features"": [""business logic"", ""dependency injection"", ""async patterns""]
    }},
    ""test"": {{
      ""language"": ""{context.TargetLanguage ?? "csharp"}"",
      ""pattern"": ""Unit Test"",
      ""features"": [""mocking"", ""assertions"", ""test data setup""]
    }}
  }},
  ""generation_rules"": {{
    ""naming_convention"": ""PascalCase"",
    ""code_style"": ""industry_standard"",
    ""documentation"": ""xml_comments"",
    ""test_coverage_target"": 85
  }}
}}";
    }
    
    private string GenerateGenerationConfig(AgentExecutionContext context)
    {
        return $@"generation:
  target_language: {context.TargetLanguage ?? "csharp"}
  output_directory: ./src/Generated
  
  templates:
    base_path: ./templates
    custom_path: ./custom_templates
    
  code_quality:
    style_rules: strict
    documentation: required
    test_coverage: 85
    
  features:
    async_patterns: true
    dependency_injection: true
    error_handling: comprehensive
    validation: automatic
    
  output_structure:
    controllers: ./Controllers
    models: ./Models
    services: ./Services
    tests: ./Tests";
    }
}