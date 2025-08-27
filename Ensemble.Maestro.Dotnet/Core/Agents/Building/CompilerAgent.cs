using Microsoft.Extensions.Logging;
using Ensemble.Maestro.Dotnet.Core.Services;

namespace Ensemble.Maestro.Dotnet.Core.Agents.Building;

/// <summary>
/// Compiler agent responsible for code compilation and optimization strategies
/// </summary>
public class CompilerAgent : BaseAgent
{
    public CompilerAgent(ILogger<BaseAgent> logger, ILLMService llmService) : base(logger, llmService) { }
    
    public override string AgentType => "Compiler";
    public override string AgentName => "Code Compiler";
    public override string Priority => "High";
    
    protected override async Task<AgentExecutionResult> ExecuteInternalAsync(AgentExecutionContext context, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Compiling code for {ProjectId}", context.ProjectId);
        
        var systemPrompt = $@"You are a code compilation specialist responsible for transforming source code into optimized executable artifacts.

Your role is to create comprehensive compilation strategies and optimization plans for various target platforms.

Target Language: {context.TargetLanguage ?? "Multi-language"}
Deployment Target: {context.DeploymentTarget ?? "Multi-platform"}

Include in your response:
• Complete compilation pipeline and strategy
• Optimization levels and techniques (performance, size, debugging)
• Cross-platform compilation approaches
• Dependency resolution and linking strategies
• Error handling and debugging support
• Build automation and CI/CD integration
• Security hardening during compilation
• Testing and validation of compiled artifacts

Provide specific, implementable compilation instructions formatted in clear markdown.";
        
        var result = await ExecuteLLMCall(systemPrompt, context, cancellationToken);
        
        if (!result.Success)
            return result;
        
        // Add compilation-specific artifacts
        result.Artifacts = new List<AgentArtifact>
        {
            new AgentArtifact
            {
                Name = "compilation_strategy.md",
                Type = "markdown",
                Content = result.OutputResponse,
                Path = "/build/compilation_strategy.md",
                Size = result.OutputResponse.Length
            },
            new AgentArtifact
            {
                Name = "build_config.yaml",
                Type = "yaml",
                Content = GenerateBuildConfig(context),
                Path = "/build/build_config.yaml",
                Size = 800
            },
            new AgentArtifact
            {
                Name = "optimization_flags.json",
                Type = "json", 
                Content = GenerateOptimizationFlags(context),
                Path = "/build/optimization_flags.json",
                Size = 600
            }
        };
        
        return result;
    }
    
    private string GenerateBuildConfig(AgentExecutionContext context)
    {
        var language = context.TargetLanguage?.ToLower() ?? "generic";
        
        return language switch
        {
            "c#" => @"# .NET Build Configuration
version: '1.0'
framework: net8.0
configuration: Release
platform: AnyCPU
optimization: true
warningsAsErrors: true
treatWarningsAsErrors: true
publishTrimmed: true
selfContained: false",

            "typescript" => @"# TypeScript Build Configuration  
version: '1.0'
target: ES2022
module: ESNext
strict: true
moduleResolution: bundler
allowImportingTsExtensions: true
noEmit: true
isolatedModules: true",

            "python" => @"# Python Build Configuration
version: '1.0'
python_version: '3.12'
build_backend: setuptools
optimization: true
bytecode_only: false
include_source: true
wheel: true",

            _ => @"# Generic Build Configuration
version: '1.0'
optimization_level: O2
debug_symbols: false
strip_binaries: true
static_linking: false"
        };
    }
    
    private string GenerateOptimizationFlags(AgentExecutionContext context)
    {
        return @"{
  ""performance"": {
    ""cpu_optimization"": ""aggressive"",
    ""memory_optimization"": ""balanced"",
    ""size_optimization"": ""moderate""
  },
  ""security"": {
    ""stack_protection"": true,
    ""fortify_source"": true,
    ""pie_enabled"": true,
    ""relro_full"": true
  },
  ""debugging"": {
    ""debug_info"": false,
    ""assertions"": false,
    ""profiling"": false
  },
  ""linking"": {
    ""strip_symbols"": true,
    ""dead_code_elimination"": true,
    ""link_time_optimization"": true
  }
}";
    }
    
    public override int GetEstimatedDurationSeconds(AgentExecutionContext context)
    {
        // Compilation planning typically takes 2-4 minutes
        var baseTime = 150; // 2.5 minutes base
        var complexity = (context.InputPrompt?.Length ?? 0) / 400;
        return baseTime + (complexity * 20);
    }
}