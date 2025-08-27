using FastEndpoints;
using Ensemble.Maestro.Dotnet.Core.Services;
using Ensemble.Maestro.Dotnet.Core.Messages;

namespace Ensemble.Maestro.Dotnet.Api.Testing;

public class TestCucsRequest
{
    public string CodeUnitName { get; set; } = "TestClass";
    public int FunctionCount { get; set; } = 3;
}

public class TestCucsResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string AssignmentId { get; set; } = string.Empty;
    public int FunctionsCreated { get; set; }
}

/// <summary>
/// Test endpoint to exercise the CUCS (Code Unit Controller Service) mechanical dispatcher
/// This creates mock CodeUnitAssignmentMessages to test the complete flow
/// </summary>
public class TestCucsEndpoint : Endpoint<TestCucsRequest, TestCucsResponse>
{
    private readonly IMessageCoordinatorService _messageCoordinator;
    private readonly ILogger<TestCucsEndpoint> _logger;

    public TestCucsEndpoint(IMessageCoordinatorService messageCoordinator, ILogger<TestCucsEndpoint> logger)
    {
        _messageCoordinator = messageCoordinator;
        _logger = logger;
    }

    public override void Configure()
    {
        Post("/api/test/cucs");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Test CUCS Mechanical Dispatcher";
            s.Description = "Creates mock CodeUnitAssignmentMessage to test the complete CUCS → MethodAgent → Builder flow";
        });
    }

    public override async Task HandleAsync(TestCucsRequest req, CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("🧪 Starting CUCS integration test for CodeUnit: {CodeUnitName} with {FunctionCount} functions", 
                req.CodeUnitName, req.FunctionCount);

            // Create mock CodeUnitAssignmentMessage
            var assignment = CreateMockCodeUnitAssignment(req.CodeUnitName, req.FunctionCount);
            
            _logger.LogInformation("📋 Created mock assignment {AssignmentId} with functions: {Functions}", 
                assignment.AssignmentId, 
                string.Join(", ", assignment.Functions.Select(f => f.FunctionName)));

            // Send to message queue (this should trigger CUCS processing)
            var success = await _messageCoordinator.SendCodeUnitAssignmentAsync(assignment, ct);
            
            if (success)
            {
                _logger.LogInformation("✅ Successfully sent CodeUnitAssignmentMessage to queue. CUCS should process it now.");
                
                await Send.OkAsync(new TestCucsResponse
                {
                    Success = true,
                    Message = $"CUCS test initiated for {req.CodeUnitName}. Check logs for processing details.",
                    AssignmentId = assignment.AssignmentId,
                    FunctionsCreated = assignment.Functions.Count
                }, ct);
            }
            else
            {
                _logger.LogError("❌ Failed to send CodeUnitAssignmentMessage to queue");
                
                await Send.ResponseAsync(new TestCucsResponse
                {
                    Success = false,
                    Message = "Failed to send assignment to message queue",
                    AssignmentId = assignment.AssignmentId,
                    FunctionsCreated = 0
                }, 500, ct);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "💥 Error in CUCS integration test");
            
            await Send.ResponseAsync(new TestCucsResponse
            {
                Success = false,
                Message = $"Test failed: {ex.Message}",
                AssignmentId = string.Empty,
                FunctionsCreated = 0
            }, 500, ct);
        }
    }

    private CodeUnitAssignmentMessage CreateMockCodeUnitAssignment(string codeUnitName, int functionCount)
    {
        var assignment = new CodeUnitAssignmentMessage
        {
            AssignmentId = Guid.NewGuid().ToString("N"),
            CodeUnitId = Guid.NewGuid().ToString("N"),
            Name = codeUnitName,
            UnitType = "Class",
            Namespace = "Ensemble.Maestro.Test",
            Description = $"Mock test class {codeUnitName} for CUCS integration testing",
            ComplexityRating = 5,
            EstimatedMinutes = functionCount * 15,
            Priority = "Medium",
            TargetLanguage = "CSharp"
        };

        // Create mock functions with varying complexity
        for (int i = 1; i <= functionCount; i++)
        {
            var complexity = (i % 3) + 3; // Complexity 3-5
            var isAsync = i % 2 == 0; // Every other function is async
            
            var function = new FunctionAssignmentMessage
            {
                AssignmentId = Guid.NewGuid().ToString("N"),
                FunctionSpecificationId = Guid.NewGuid().ToString("N"),
                FunctionName = $"Process{codeUnitName}Item{i}",
                CodeUnit = codeUnitName,
                Signature = isAsync 
                    ? $"public async Task<bool> Process{codeUnitName}Item{i}(string input, int count)" 
                    : $"public string Process{codeUnitName}Item{i}(string input)",
                Description = $"Mock function {i} for testing CUCS dispatcher - complexity {complexity}",
                BusinessLogic = $"Process input data for item {i} using business rules",
                ValidationRules = "Validate input is not null or empty",
                ErrorHandling = "Return appropriate error responses for invalid inputs",
                SecurityConsiderations = "Sanitize input parameters",
                ComplexityRating = complexity,
                EstimatedMinutes = complexity * 5,
                Priority = i == 1 ? "High" : "Medium", // First function gets high priority
                TargetLanguage = "CSharp"
            };

            assignment.Functions.Add(function);
        }

        assignment.SimpleFunctionCount = assignment.Functions.Count(f => f.ComplexityRating <= 3);
        assignment.ComplexFunctionCount = assignment.Functions.Count(f => f.ComplexityRating > 3);

        return assignment;
    }
}