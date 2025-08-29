using FastEndpoints;
using Microsoft.Extensions.DependencyInjection;
using Ensemble.Maestro.Dotnet.Core.Services;
using Ensemble.Maestro.Dotnet.Core.Messages;
using Ensemble.Maestro.Dotnet.Core.Agents;

namespace Ensemble.Maestro.Dotnet.Api.Test;

/// <summary>
/// Direct MethodAgent testing endpoint for isolated debugging of individual MethodAgent execution
/// Tests AgentFactory → MethodAgent → ExecuteAsync flow directly
/// </summary>
public class DirectMethodAgentTestEndpoint : EndpointWithoutRequest<DirectMethodAgentTestResponse>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DirectMethodAgentTestEndpoint> _logger;

    public DirectMethodAgentTestEndpoint(IServiceProvider serviceProvider, ILogger<DirectMethodAgentTestEndpoint> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public override void Configure()
    {
        Post("/api/test/methodagent/direct");
        AllowAnonymous();
        Summary(s => {
            s.Summary = "Direct MethodAgent Test - isolated testing of MethodAgent creation and execution";
            s.Description = "Creates MethodJobPacket and directly tests AgentFactory → MethodAgent → ExecuteAsync flow for debugging";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        _logger.LogInformation("🧪 DIRECT METHODAGENT TEST ENDPOINT - Starting isolated MethodAgent testing");

        var response = new DirectMethodAgentTestResponse
        {
            TestStarted = DateTime.UtcNow,
            Steps = new List<TestStep>()
        };

        try
        {
            // Step 1: Create scope and resolve services
            response.Steps.Add(new TestStep { Step = "1", Description = "Create scope and resolve services", Status = "Starting", Timestamp = DateTime.UtcNow });
            
            using var scope = _serviceProvider.CreateScope();
            var agentFactory = scope.ServiceProvider.GetRequiredService<IAgentFactory>();
            
            response.Steps.Last().Status = "Success";
            response.Steps.Last().Details = "Successfully resolved IAgentFactory service";
            _logger.LogInformation("✅ Step 1 completed - Services resolved");

            // Step 2: Test AgentFactory.CreateAgent for MethodAgent
            response.Steps.Add(new TestStep { Step = "2", Description = "Test AgentFactory.CreateAgent(\"MethodAgent\")", Status = "Starting", Timestamp = DateTime.UtcNow });
            
            _logger.LogInformation("🏭 Calling AgentFactory.CreateAgent(\"MethodAgent\")...");
            var agent = agentFactory.CreateAgent("MethodAgent");
            _logger.LogInformation("🔍 AgentFactory returned: {AgentType} (IsNull: {IsNull})", 
                agent?.GetType().Name ?? "NULL", agent == null);
            
            response.Steps.Last().Status = agent != null ? "Success" : "Failed";
            response.Steps.Last().Details = $"AgentFactory returned: {agent?.GetType().Name ?? "NULL"}";
            
            if (agent == null)
            {
                throw new InvalidOperationException("AgentFactory returned null for MethodAgent");
            }

            // Step 3: Test cast to IMethodAgent
            response.Steps.Add(new TestStep { Step = "3", Description = "Test cast to IMethodAgent interface", Status = "Starting", Timestamp = DateTime.UtcNow });
            
            var methodAgent = agent as IMethodAgent;
            _logger.LogInformation("🔄 Cast to IMethodAgent: {MethodAgentType} (IsNull: {IsNull})", 
                methodAgent?.GetType().Name ?? "NULL", methodAgent == null);
            
            response.Steps.Last().Status = methodAgent != null ? "Success" : "Failed";
            response.Steps.Last().Details = $"IMethodAgent cast result: {methodAgent?.GetType().Name ?? "NULL"}";
            
            if (methodAgent == null)
            {
                throw new InvalidOperationException($"Failed to cast {agent.GetType().Name} to IMethodAgent");
            }

            // Step 4: Create MethodJobPacket
            response.Steps.Add(new TestStep { Step = "4", Description = "Create test MethodJobPacket", Status = "Starting", Timestamp = DateTime.UtcNow });
            
            var jobPacket = CreateTestJobPacket();
            
            response.Steps.Last().Status = "Success";
            response.Steps.Last().Details = $"Created JobPacket - JobId: {jobPacket.JobId}, Function: {jobPacket.Function.Name}, Priority: {jobPacket.Priority}";
            _logger.LogInformation("📦 Step 4 completed - JobPacket created: JobId: {JobId}, Function: {FunctionName}", 
                jobPacket.JobId, jobPacket.Function.Name);

            // Step 5: Execute MethodAgent.ExecuteAsync
            response.Steps.Add(new TestStep { Step = "5", Description = "Execute MethodAgent.ExecuteAsync(jobPacket)", Status = "Starting", Timestamp = DateTime.UtcNow });
            
            _logger.LogInformation("🚀 Calling MethodAgent.ExecuteAsync with JobId: {JobId}, Function: {FunctionName}...", 
                jobPacket.JobId, jobPacket.Function.Name);
            
            var startTime = DateTime.UtcNow;
            var result = await methodAgent.ExecuteAsync(jobPacket);
            var duration = (DateTime.UtcNow - startTime).TotalSeconds;
            
            _logger.LogInformation("✅ MethodAgent.ExecuteAsync completed - Success: {Success}, Duration: {Duration}s, Output length: {OutputLength}", 
                result?.Success ?? false, duration, result?.OutputResponse?.Length ?? 0);
            
            response.Steps.Last().Status = result?.Success == true ? "Success" : "Failed";
            response.Steps.Last().Details = $"Success: {result?.Success ?? false}, Duration: {duration:F2}s, Output: {(result?.OutputResponse?.Length ?? 0)} chars";
            
            response.ExecutionResult = new MethodAgentExecutionResult
            {
                Success = result?.Success ?? false,
                OutputResponse = result?.OutputResponse ?? string.Empty,
                QualityScore = result?.QualityScore ?? 0,
                ConfidenceScore = result?.ConfidenceScore ?? 0,
                InputTokens = result?.InputTokens ?? 0,
                OutputTokens = result?.OutputTokens ?? 0,
                DurationSeconds = (int)duration,
                ErrorMessage = result?.ErrorMessage
            };

            if (result?.Success != true)
            {
                throw new InvalidOperationException($"MethodAgent execution failed: {result?.ErrorMessage ?? "Unknown error"}");
            }

            response.Success = true;
            response.Message = "Direct MethodAgent test completed successfully";
            _logger.LogInformation("🎉 DIRECT METHODAGENT TEST COMPLETED SUCCESSFULLY");
        }
        catch (Exception ex)
        {
            response.Success = false;
            response.Message = $"Direct MethodAgent test failed: {ex.Message}";
            
            if (response.Steps.Any())
            {
                response.Steps.Last().Status = "Failed";
                response.Steps.Last().Details = $"Exception: {ex.GetType().Name} - {ex.Message}";
            }
            
            _logger.LogError(ex, "💥 DIRECT METHODAGENT TEST FAILED - Exception: {ExceptionType}, Message: {ExceptionMessage}, Stack: {StackTrace}",
                ex.GetType().Name, ex.Message, ex.StackTrace);
        }
        finally
        {
            response.TestCompleted = DateTime.UtcNow;
            response.DurationSeconds = (int)(response.TestCompleted.Value - response.TestStarted).TotalSeconds;
        }

        await Send.OkAsync(response, ct);
    }

    private MethodJobPacket CreateTestJobPacket()
    {
        return new MethodJobPacket
        {
            JobId = Guid.NewGuid().ToString("N"),
            ProjectId = "TEST-PROJECT-001",
            CodeUnitName = "TestCodeUnit",
            Function = new FunctionSpec
            {
                Name = "CalculateTotal",
                ReturnType = "decimal",
                Parameters = new List<ParameterSpec>
                {
                    new ParameterSpec { Name = "items", Type = "List<decimal>" },
                    new ParameterSpec { Name = "tax", Type = "decimal" }
                },
                AccessModifier = "public",
                IsStatic = false,
                IsAsync = false,
                Description = "Calculates the total cost including tax for a list of item prices"
            },
            CreatedAt = DateTime.UtcNow,
            Priority = 7,
            Context = new Dictionary<string, object>
            {
                ["codeUnitName"] = "TestCodeUnit",
                ["projectId"] = "TEST-PROJECT-001",
                ["functionComplexity"] = 4
            }
        };
    }
}

public class DirectMethodAgentTestResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime TestStarted { get; set; }
    public DateTime? TestCompleted { get; set; }
    public int DurationSeconds { get; set; }
    public List<TestStep> Steps { get; set; } = new();
    public MethodAgentExecutionResult? ExecutionResult { get; set; }
}

public class MethodAgentExecutionResult
{
    public bool Success { get; set; }
    public string OutputResponse { get; set; } = string.Empty;
    public int QualityScore { get; set; }
    public int ConfidenceScore { get; set; }
    public int InputTokens { get; set; }
    public int OutputTokens { get; set; }
    public int DurationSeconds { get; set; }
    public string? ErrorMessage { get; set; }
}