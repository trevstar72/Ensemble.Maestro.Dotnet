using Microsoft.Extensions.Logging;
using Ensemble.Maestro.Dotnet.Core.Services;
using Ensemble.Maestro.Dotnet.Core.Messages;

namespace Ensemble.Maestro.Dotnet.Core.Agents.Swarming;

/// <summary>
/// Specialized agent for implementing a single function/method with high focus and quality
/// Method Agents are spawned by Code Unit Controller Service (CUCS) for individual function implementation
/// Produces individual code documents (not monolithic reports) for CUCS collection
/// </summary>
public class MethodAgent : BaseAgent, IMethodAgent
{
    private readonly IMessageCoordinatorService _messageCoordinator;
    private readonly ISwarmConfigurationService _swarmConfig;

    public MethodAgent(
        ILogger<MethodAgent> logger, 
        ILLMService llmService,
        IMessageCoordinatorService messageCoordinator,
        ISwarmConfigurationService swarmConfig)
        : base(logger, llmService)
    {
        _messageCoordinator = messageCoordinator;
        _swarmConfig = swarmConfig;
    }

    public override string AgentType => "MethodAgent";
    public override string AgentName => "Method Agent";
    public override string Priority => "Normal";

    /// <summary>
    /// Validate that we have a function assignment
    /// </summary>
    public override bool CanExecute(AgentExecutionContext context)
    {
        return base.CanExecute(context) && 
               context.Metadata.ContainsKey("FunctionId") &&
               context.Metadata.ContainsKey("CodeUnitId");
    }

    /// <summary>
    /// Main execution logic for implementing a single function
    /// </summary>
    protected override async Task<AgentExecutionResult> ExecuteInternalAsync(
        AgentExecutionContext context, 
        CancellationToken cancellationToken)
    {
        var functionId = Guid.Parse(context.Metadata["FunctionId"].ToString()!);
        var codeUnitId = Guid.Parse(context.Metadata["CodeUnitId"].ToString()!);
        var complexityRating = context.Metadata.ContainsKey("ComplexityRating") 
            ? Convert.ToInt32(context.Metadata["ComplexityRating"]) 
            : 5;

        try
        {
            _logger.LogInformation("Method Agent starting implementation of function {FunctionId} in code unit {CodeUnitId}", 
                functionId, codeUnitId);
            
            // Step 1: Analyze the function requirements in detail
            var analysisResult = await AnalyzeFunctionRequirementsAsync(context, cancellationToken);
            if (!analysisResult.Success)
            {
                await SendCompletionMessageAsync(context, false, analysisResult.ErrorMessage ?? "Analysis failed", cancellationToken);
                return analysisResult;
            }

            // Step 2: Design the function implementation approach
            var designResult = await DesignImplementationApproachAsync(context, analysisResult.OutputResponse, cancellationToken);
            if (!designResult.Success)
            {
                await SendCompletionMessageAsync(context, false, designResult.ErrorMessage ?? "Design failed", cancellationToken);
                return designResult;
            }

            // Step 3: Generate the actual implementation
            var implementationResult = await GenerateImplementationAsync(context, designResult.OutputResponse, cancellationToken);
            if (!implementationResult.Success)
            {
                await SendCompletionMessageAsync(context, false, implementationResult.ErrorMessage ?? "Implementation failed", cancellationToken);
                return implementationResult;
            }

            // Step 4: Validate and test the implementation
            var validationResult = await ValidateImplementationAsync(context, implementationResult.OutputResponse, cancellationToken);
            
            // Step 5: Create final result with comprehensive output
            var finalOutput = await CreateFinalOutputAsync(
                context, analysisResult, designResult, implementationResult, validationResult, cancellationToken);

            // Step 6: Send completion message to coordinator
            await SendCompletionMessageAsync(context, true, "Function implementation completed successfully", cancellationToken);

            var (quality, confidence) = AnalyzeImplementationQuality(implementationResult.OutputResponse, context, complexityRating);

            return new AgentExecutionResult
            {
                Success = true,
                OutputResponse = finalOutput,
                QualityScore = quality,
                ConfidenceScore = confidence,
                InputTokens = analysisResult.InputTokens + designResult.InputTokens + implementationResult.InputTokens,
                OutputTokens = analysisResult.OutputTokens + designResult.OutputTokens + implementationResult.OutputTokens,
                ExecutionCost = analysisResult.ExecutionCost + designResult.ExecutionCost + implementationResult.ExecutionCost,
                DurationSeconds = (int)(DateTime.UtcNow - context.StartTime).TotalSeconds,
                Metadata = new Dictionary<string, object>
                {
                    { "FunctionId", functionId },
                    { "CodeUnitId", codeUnitId },
                    { "ComplexityRating", complexityRating },
                    { "ImplementationApproach", "Multi-stage with validation" },
                    { "ValidationPassed", validationResult.Success }
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Method Agent for function {FunctionId}", functionId);
            await SendCompletionMessageAsync(context, false, $"Method Agent failed: {ex.Message}", cancellationToken);
            return AgentExecutionResult.Failure($"Method Agent failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Analyze function requirements in detail
    /// </summary>
    private async Task<AgentExecutionResult> AnalyzeFunctionRequirementsAsync(
        AgentExecutionContext context,
        CancellationToken cancellationToken)
    {
        var systemPrompt = $@"You are a Method Agent specializing in deep analysis of individual function requirements.

Your task is to thoroughly analyze the function specification provided and create a comprehensive understanding of what needs to be implemented.

Analyze the following aspects:
1. **Function Purpose**: What is the core responsibility of this function?
2. **Input Analysis**: Detailed breakdown of all input parameters, their types, constraints, and validation requirements
3. **Output Analysis**: Expected return values, error conditions, and success criteria
4. **Edge Cases**: Identify potential edge cases and error scenarios
5. **Dependencies**: What external dependencies, services, or data structures are needed?
6. **Performance Considerations**: Any performance requirements or optimization opportunities
7. **Security Considerations**: Input validation, authorization, or security requirements
8. **Testing Strategy**: What would need to be tested to ensure correctness?

Provide a detailed analysis that will guide the implementation design.

Target Language: {context.TargetLanguage}
Complexity Rating: {context.Metadata.GetValueOrDefault("ComplexityRating", 5)}

Be thorough and precise in your analysis.";

        return await ExecuteLLMCall(systemPrompt, context, cancellationToken);
    }

    /// <summary>
    /// Design the implementation approach
    /// </summary>
    private async Task<AgentExecutionResult> DesignImplementationApproachAsync(
        AgentExecutionContext context,
        string analysisResult,
        CancellationToken cancellationToken)
    {
        var designContext = new AgentExecutionContext
        {
            InputPrompt = $@"Based on the following detailed analysis, design a comprehensive implementation approach:

ANALYSIS RESULTS:
{analysisResult}

ORIGINAL REQUIREMENTS:
{context.InputPrompt}

Design the implementation approach covering:
1. **Algorithm/Logic Flow**: Step-by-step approach to implement the function
2. **Data Structures**: What data structures or classes are needed?
3. **Error Handling Strategy**: How to handle different error conditions gracefully
4. **Validation Logic**: Input validation and constraint checking
5. **Performance Optimizations**: Any optimizations for efficiency
6. **Code Structure**: How to organize the code for maintainability
7. **Integration Points**: How this function integrates with the broader system
8. **Testing Approach**: Unit test scenarios and test data needed

Provide a detailed design that serves as a blueprint for implementation.",
            ProjectId = context.ProjectId,
            PipelineExecutionId = context.PipelineExecutionId,
            ExecutionId = context.ExecutionId,
            TargetLanguage = context.TargetLanguage,
            MaxTokens = context.MaxTokens,
            Temperature = 0.3f, // Lower temperature for more structured design
            Metadata = context.Metadata
        };

        var systemPrompt = $@"You are a Method Agent designing the implementation approach for a complex function.

Create a detailed, actionable design that will guide the implementation phase. Focus on clarity, completeness, and practical implementation considerations.

Target Language: {context.TargetLanguage}
Complexity Rating: {context.Metadata.GetValueOrDefault("ComplexityRating", 5)}

Provide a structured design document.";

        return await ExecuteLLMCall(systemPrompt, designContext, cancellationToken);
    }

    /// <summary>
    /// Generate the actual implementation
    /// </summary>
    private async Task<AgentExecutionResult> GenerateImplementationAsync(
        AgentExecutionContext context,
        string designDocument,
        CancellationToken cancellationToken)
    {
        var implementationContext = new AgentExecutionContext
        {
            InputPrompt = $@"Implement the function based on the following design document:

DESIGN DOCUMENT:
{designDocument}

ORIGINAL REQUIREMENTS:
{context.InputPrompt}

Generate a complete, production-ready implementation that:
1. Follows the design approach exactly
2. Includes comprehensive error handling
3. Has proper input validation
4. Includes clear, meaningful comments
5. Follows {context.TargetLanguage} best practices and conventions
6. Is optimized for performance where appropriate
7. Is maintainable and readable
8. Includes appropriate logging (if applicable)

Provide ONLY the function implementation code, properly formatted and complete.",
            ProjectId = context.ProjectId,
            PipelineExecutionId = context.PipelineExecutionId,
            ExecutionId = context.ExecutionId,
            TargetLanguage = context.TargetLanguage,
            MaxTokens = context.MaxTokens,
            Temperature = 0.2f, // Even lower temperature for implementation consistency
            Metadata = context.Metadata
        };

        var systemPrompt = $@"You are a Method Agent implementing a complex function based on detailed analysis and design.

Generate high-quality, production-ready code that exactly follows the provided design document.

Requirements:
- Complete implementation with no placeholders
- Comprehensive error handling
- Input validation and sanitization  
- Clear documentation and comments
- Follow {context.TargetLanguage} best practices
- Optimize for both performance and readability
- Include proper logging where appropriate

Return only the complete function implementation.";

        return await ExecuteLLMCall(systemPrompt, implementationContext, cancellationToken);
    }

    /// <summary>
    /// Validate the implementation
    /// </summary>
    private async Task<AgentExecutionResult> ValidateImplementationAsync(
        AgentExecutionContext context,
        string implementation,
        CancellationToken cancellationToken)
    {
        var validationContext = new AgentExecutionContext
        {
            InputPrompt = $@"Validate the following function implementation against the original requirements:

IMPLEMENTATION:
```{context.TargetLanguage?.ToLower()}
{implementation}
```

ORIGINAL REQUIREMENTS:
{context.InputPrompt}

Perform a comprehensive validation covering:
1. **Correctness**: Does it meet the functional requirements?
2. **Error Handling**: Are error cases properly handled?
3. **Input Validation**: Are inputs properly validated?
4. **Code Quality**: Is the code well-structured and maintainable?
5. **Performance**: Are there any performance issues?
6. **Security**: Are there any security vulnerabilities?
7. **Best Practices**: Does it follow {context.TargetLanguage} conventions?
8. **Edge Cases**: Are edge cases properly handled?

Provide:
- Validation status (PASS/FAIL)
- List of issues found (if any)
- Suggestions for improvement
- Test scenarios that should be covered",
            ProjectId = context.ProjectId,
            PipelineExecutionId = context.PipelineExecutionId,
            ExecutionId = context.ExecutionId,
            TargetLanguage = context.TargetLanguage,
            MaxTokens = 1500,
            Temperature = 0.1f, // Lowest temperature for validation accuracy
            Metadata = context.Metadata
        };

        var systemPrompt = $@"You are a Method Agent performing final validation of a function implementation.

Conduct a thorough code review and validation to ensure the implementation is production-ready.

Be comprehensive and identify any issues that could cause problems in production.

Target Language: {context.TargetLanguage}";

        return await ExecuteLLMCall(systemPrompt, validationContext, cancellationToken);
    }

    /// <summary>
    /// Create individual code document output (not monolithic report)
    /// This now produces individual code documents as required by CUCS architecture
    /// </summary>
    private async Task<string> CreateFinalOutputAsync(
        AgentExecutionContext context,
        AgentExecutionResult analysis,
        AgentExecutionResult design,
        AgentExecutionResult implementation,
        AgentExecutionResult validation,
        CancellationToken cancellationToken)
    {
        var functionName = context.Metadata.ContainsKey("FunctionName") 
            ? context.Metadata["FunctionName"].ToString()
            : context.Metadata.GetValueOrDefault("FunctionId", "UnknownFunction").ToString();

        // Return only the clean implementation code as individual document
        // This allows the CUCS to collect individual code documents rather than monolithic reports
        return implementation.OutputResponse;
    }

    /// <summary>
    /// Send completion message to the swarm coordinator
    /// </summary>
    private async Task SendCompletionMessageAsync(
        AgentExecutionContext context,
        bool success,
        string message,
        CancellationToken cancellationToken)
    {
        try
        {
            var completionMessage = new AgentCompletionMessage
            {
                AgentId = context.ExecutionId.ToString(),
                AgentType = AgentType,
                Success = success,
                ErrorMessage = success ? null : message,
                OutputResponse = success ? message : string.Empty,
                CompletedAt = DateTime.UtcNow
            };

            await _messageCoordinator.SendAgentCompletionAsync(completionMessage, cancellationToken);
            
            _logger.LogInformation("Sent completion message for Method Agent {AgentId}: {Success}", 
                context.ExecutionId, success);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send completion message for Method Agent {AgentId}", context.ExecutionId);
        }
    }

    /// <summary>
    /// Analyze implementation quality based on complexity and code characteristics
    /// </summary>
    private (int quality, int confidence) AnalyzeImplementationQuality(
        string implementation, 
        AgentExecutionContext context, 
        int complexityRating)
    {
        var quality = 70; // Base quality for Method Agent implementations
        var confidence = 75; // Base confidence

        // Length-based scoring (appropriate depth for complexity)
        var expectedLength = complexityRating * 200; // ~200 chars per complexity point
        var lengthRatio = (double)implementation.Length / expectedLength;
        
        if (lengthRatio > 0.8 && lengthRatio < 2.0) quality += 10; // Appropriate length
        else if (lengthRatio > 0.5 && lengthRatio < 3.0) quality += 5; // Reasonable length
        
        // Structure and quality indicators
        if (implementation.Contains("try") && implementation.Contains("catch")) quality += 8; // Error handling
        if (implementation.Contains("if") && implementation.Contains("else")) quality += 5; // Conditional logic
        if (implementation.Contains("//") || implementation.Contains("/*")) quality += 7; // Comments
        if (implementation.Contains("throw") || implementation.Contains("return")) quality += 5; // Proper flow control
        
        // Validation patterns based on language
        if (context.TargetLanguage?.ToLower() == "c#")
        {
            if (implementation.Contains("ArgumentNullException") || implementation.Contains("ArgumentException")) quality += 8;
            if (implementation.Contains("async") && implementation.Contains("await")) quality += 6;
            if (implementation.Contains("ILogger") || implementation.Contains("_logger")) quality += 5;
        }
        
        // Complexity alignment - higher complexity should show more sophisticated patterns
        if (complexityRating >= 7)
        {
            var sophisticatedPatterns = new[] { "interface", "class", "abstract", "virtual", "override", "generic" };
            var foundPatterns = sophisticatedPatterns.Count(pattern => 
                implementation.Contains(pattern, StringComparison.OrdinalIgnoreCase));
            quality += foundPatterns * 3;
        }
        
        // Confidence based on comprehensive approach
        confidence += Math.Min(20, complexityRating * 2); // Higher complexity = higher confidence when properly handled
        
        // Penalize if implementation seems too simple for complexity
        if (complexityRating >= 6 && implementation.Length < 500)
        {
            quality -= 15;
            confidence -= 10;
        }
        
        return (Math.Min(100, Math.Max(0, quality)), Math.Min(100, Math.Max(0, confidence)));
    }

    /// <summary>
    /// Get expected sections for quality assessment
    /// </summary>
    protected override string[] GetExpectedSectionsForAgent()
    {
        return new[] { "implementation", "analysis", "design", "validation", "function" };
    }

    /// <summary>
    /// Get estimated duration based on complexity
    /// </summary>
    public override int GetEstimatedDurationSeconds(AgentExecutionContext context)
    {
        var complexityRating = context.Metadata.ContainsKey("ComplexityRating") 
            ? Convert.ToInt32(context.Metadata["ComplexityRating"]) 
            : 5;
        
        // Base time for analysis + design + implementation + validation
        var baseTime = 60; // 1 minute base
        var complexityTime = complexityRating * 30; // 30 seconds per complexity point
        
        return baseTime + complexityTime;
    }

    /// <summary>
    /// Execute with MethodJobPacket (IMethodAgent interface implementation)
    /// This allows CUCS to call MethodAgent with job packets
    /// </summary>
    public async Task<AgentExecutionResult> ExecuteAsync(MethodJobPacket jobPacket)
    {
        // Convert MethodJobPacket to AgentExecutionContext for compatibility
        var context = new AgentExecutionContext
        {
            InputPrompt = $@"Implement the following function:

Function Name: {jobPacket.Function.Name}
Return Type: {jobPacket.Function.ReturnType}
Parameters: {string.Join(", ", jobPacket.Function.Parameters?.Select(p => $"{p.Type} {p.Name}") ?? new string[0])}
Access Modifier: {jobPacket.Function.AccessModifier}
Is Static: {jobPacket.Function.IsStatic}
Is Async: {jobPacket.Function.IsAsync}

Function Description: {jobPacket.Function.Description}

Requirements:
- Follow best practices for {jobPacket.Function.ReturnType} functions
- Include proper error handling and validation
- Add meaningful comments
- Ensure the implementation is production-ready",
            
            ProjectId = Guid.Parse(jobPacket.ProjectId),
            PipelineExecutionId = Guid.NewGuid(),
            ExecutionId = Guid.Parse(jobPacket.JobId),
            TargetLanguage = "C#",
            MaxTokens = 4000,
            Temperature = 0.3f,
            Metadata = new Dictionary<string, object>
            {
                ["FunctionId"] = jobPacket.JobId,
                ["FunctionName"] = jobPacket.Function.Name,
                ["CodeUnitId"] = jobPacket.CodeUnitName,
                ["CodeUnitName"] = jobPacket.CodeUnitName,
                ["ComplexityRating"] = jobPacket.Context.GetValueOrDefault("functionComplexity", 5),
                ["Priority"] = jobPacket.Priority
            },
            StartTime = DateTime.UtcNow
        };

        // Execute using the standard execution flow
        return await ExecuteAsync(context, CancellationToken.None);
    }
}