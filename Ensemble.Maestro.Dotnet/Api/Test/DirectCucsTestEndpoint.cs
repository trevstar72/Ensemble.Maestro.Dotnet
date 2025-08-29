using FastEndpoints;
using Microsoft.Extensions.DependencyInjection;
using Ensemble.Maestro.Dotnet.Core.Services;
using Ensemble.Maestro.Dotnet.Core.Messages;
using Ensemble.Maestro.Dotnet.Core.Agents;

namespace Ensemble.Maestro.Dotnet.Api.Test;

/// <summary>
/// Direct CUCS testing endpoint for isolated debugging of CodeUnitControllerService
/// Bypasses Redis queue and directly tests CUCS processing
/// </summary>
public class DirectCucsTestEndpoint : EndpointWithoutRequest<DirectCucsTestResponse>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DirectCucsTestEndpoint> _logger;

    public DirectCucsTestEndpoint(IServiceProvider serviceProvider, ILogger<DirectCucsTestEndpoint> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public override void Configure()
    {
        Post("/api/test/cucs/direct");
        AllowAnonymous();
        Summary(s => {
            s.Summary = "Direct CUCS Test - bypasses Redis and directly tests CUCS message processing";
            s.Description = "Creates mock CodeUnitAssignmentMessage and directly calls CUCS ProcessCodeUnitAssignmentAsync for isolated debugging";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        _logger.LogInformation("🧪 DIRECT CUCS TEST ENDPOINT - Starting isolated CUCS testing");

        var response = new DirectCucsTestResponse
        {
            TestStarted = DateTime.UtcNow,
            Steps = new List<TestStep>()
        };

        try
        {
            // Step 1: Create scope and resolve CUCS
            response.Steps.Add(new TestStep { Step = "1", Description = "Create scope and resolve CUCS", Status = "Starting", Timestamp = DateTime.UtcNow });
            
            using var scope = _serviceProvider.CreateScope();
            var cucs = scope.ServiceProvider.GetRequiredService<CodeUnitControllerService>();
            var agentFactory = scope.ServiceProvider.GetRequiredService<IAgentFactory>();
            
            response.Steps.Last().Status = "Success";
            response.Steps.Last().Details = "Successfully resolved CodeUnitControllerService and AgentFactory";
            _logger.LogInformation("✅ Step 1 completed - CUCS and AgentFactory resolved");

            // Step 2: Test AgentFactory directly
            response.Steps.Add(new TestStep { Step = "2", Description = "Test AgentFactory MethodAgent creation", Status = "Starting", Timestamp = DateTime.UtcNow });
            
            var methodAgent = agentFactory.CreateAgent("MethodAgent");
            var isMethodAgent = methodAgent is IMethodAgent;
            
            response.Steps.Last().Status = isMethodAgent ? "Success" : "Failed";
            response.Steps.Last().Details = $"AgentFactory returned: {methodAgent?.GetType().Name ?? "NULL"}, IMethodAgent cast: {isMethodAgent}";
            _logger.LogInformation("🔧 Step 2 completed - MethodAgent creation test: {Status}", response.Steps.Last().Status);

            // Step 3: Create mock CodeUnitAssignmentMessage
            response.Steps.Add(new TestStep { Step = "3", Description = "Create mock CodeUnitAssignmentMessage", Status = "Starting", Timestamp = DateTime.UtcNow });
            
            var mockAssignment = CreateMockCodeUnitAssignment();
            
            response.Steps.Last().Status = "Success";
            response.Steps.Last().Details = $"Created assignment for CodeUnit: {mockAssignment.Name} with {mockAssignment.Functions.Count} functions";
            _logger.LogInformation("📦 Step 3 completed - Mock assignment created: {CodeUnitName}, {FunctionCount} functions", 
                mockAssignment.Name, mockAssignment.Functions.Count);

            // Step 4: Test CUCS ProcessCodeUnitAssignmentAsync directly
            response.Steps.Add(new TestStep { Step = "4", Description = "Call CUCS ProcessCodeUnitAssignmentAsync directly", Status = "Starting", Timestamp = DateTime.UtcNow });
            
            _logger.LogInformation("🚀 Step 4 - Calling CUCS.ProcessCodeUnitAssignmentAsync directly...");
            await cucs.ProcessCodeUnitAssignmentAsync(mockAssignment);
            
            response.Steps.Last().Status = "Success";
            response.Steps.Last().Details = "CUCS ProcessCodeUnitAssignmentAsync completed without exceptions";
            _logger.LogInformation("✅ Step 4 completed - CUCS processing completed successfully");

            // Step 5: Wait and check for outputs
            response.Steps.Add(new TestStep { Step = "5", Description = "Wait for processing and check outputs", Status = "Starting", Timestamp = DateTime.UtcNow });
            
            // Wait a few seconds for processing to complete
            await Task.Delay(5000, ct);
            
            response.Steps.Last().Status = "Success";
            response.Steps.Last().Details = "Waited 5 seconds for processing completion";
            _logger.LogInformation("⏳ Step 5 completed - Processing delay completed");

            response.Success = true;
            response.Message = "Direct CUCS test completed successfully";
            _logger.LogInformation("🎉 DIRECT CUCS TEST COMPLETED SUCCESSFULLY");
        }
        catch (Exception ex)
        {
            response.Success = false;
            response.Message = $"Direct CUCS test failed: {ex.Message}";
            
            if (response.Steps.Any())
            {
                response.Steps.Last().Status = "Failed";
                response.Steps.Last().Details = $"Exception: {ex.GetType().Name} - {ex.Message}";
            }
            
            _logger.LogError(ex, "💥 DIRECT CUCS TEST FAILED - Exception: {ExceptionType}, Message: {ExceptionMessage}, Stack: {StackTrace}",
                ex.GetType().Name, ex.Message, ex.StackTrace);
        }
        finally
        {
            response.TestCompleted = DateTime.UtcNow;
            response.DurationSeconds = (int)(response.TestCompleted.Value - response.TestStarted).TotalSeconds;
        }

        await Send.OkAsync(response, ct);
    }

    private CodeUnitAssignmentMessage CreateMockCodeUnitAssignment()
    {
        return new CodeUnitAssignmentMessage
        {
            AssignmentId = Guid.NewGuid().ToString("N"),
            CodeUnitId = "TEST-CU-001",
            Name = "TestCodeUnit",
            Priority = "High",
            TargetLanguage = "C#",
            DueAt = DateTime.UtcNow.AddHours(1),
            Functions = new List<FunctionAssignmentMessage>
            {
                new FunctionAssignmentMessage
                {
                    FunctionName = "TestMethodOne",
                    Signature = "public async Task<string> TestMethodOne(int id, string name)",
                    Description = "Test method for CUCS debugging - returns a formatted string based on input parameters",
                    ComplexityRating = 3,
                    Priority = "High"
                },
                new FunctionAssignmentMessage
                {
                    FunctionName = "TestMethodTwo",
                    Signature = "public bool ValidateInput(object input)",
                    Description = "Test method for input validation - checks if input is valid and not null",
                    ComplexityRating = 2,
                    Priority = "Medium"
                }
            }
        };
    }
}

public class DirectCucsTestResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime TestStarted { get; set; }
    public DateTime? TestCompleted { get; set; }
    public int DurationSeconds { get; set; }
    public List<TestStep> Steps { get; set; } = new();
}

public class TestStep
{
    public string Step { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}