using FastEndpoints;
using Ensemble.Maestro.Dotnet.Core.Services;
using Ensemble.Maestro.Dotnet.Core.Messages;

namespace Ensemble.Maestro.Dotnet.Api.Testing;

public class DirectTestCucsRequest
{
    public string CodeUnitName { get; set; } = "DirectTestClass";
    public int FunctionCount { get; set; } = 2;
}

public class DirectTestCucsResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string AssignmentId { get; set; } = string.Empty;
    public int FunctionsProcessed { get; set; }
    public List<string> ProcessingLogs { get; set; } = new();
}

/// <summary>
/// Direct test endpoint that bypasses Redis and directly invokes CUCS for testing
/// This allows us to validate the complete CUCS → MethodAgent → Individual Code Documents flow
/// </summary>
public class DirectTestCucsEndpoint : Endpoint<DirectTestCucsRequest, DirectTestCucsResponse>
{
    private readonly CodeUnitControllerService _cucs;
    private readonly ILogger<DirectTestCucsEndpoint> _logger;

    public DirectTestCucsEndpoint(CodeUnitControllerService cucs, ILogger<DirectTestCucsEndpoint> logger)
    {
        _cucs = cucs;
        _logger = logger;
    }

    public override void Configure()
    {
        Post("/api/test/cucs-direct");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Direct CUCS Test (No Redis)";
            s.Description = "Directly tests CUCS mechanical dispatcher without Redis, showing complete MethodAgent spawning and individual code document generation";
        });
    }

    public override async Task HandleAsync(DirectTestCucsRequest req, CancellationToken ct)
    {
        var logs = new List<string>();
        
        try
        {
            logs.Add($"🎯 Starting DIRECT CUCS test for {req.CodeUnitName} with {req.FunctionCount} functions");
            _logger.LogInformation("🎯 DIRECT CUCS TEST: Starting for {CodeUnitName} with {FunctionCount} functions", 
                req.CodeUnitName, req.FunctionCount);

            // Create mock CodeUnitAssignmentMessage
            var assignment = CreateMockCodeUnitAssignment(req.CodeUnitName, req.FunctionCount);
            logs.Add($"📋 Created assignment {assignment.AssignmentId}");
            logs.Add($"📋 Functions: {string.Join(", ", assignment.Functions.Select(f => f.FunctionName))}");
            
            _logger.LogInformation("📋 Created mock assignment with functions: {Functions}", 
                string.Join(", ", assignment.Functions.Select(f => f.FunctionName)));

            // DIRECTLY invoke CUCS (bypassing Redis)
            logs.Add("🚀 Directly invoking CUCS.ProcessCodeUnitAssignmentAsync...");
            
            await _cucs.ProcessCodeUnitAssignmentAsync(assignment);
            
            logs.Add("✅ CUCS processing completed!");
            logs.Add($"🎉 Expected to see {assignment.Functions.Count} individual code documents generated");
            
            await Send.OkAsync(new DirectTestCucsResponse
            {
                Success = true,
                Message = $"DIRECT CUCS test completed successfully for {req.CodeUnitName}",
                AssignmentId = assignment.AssignmentId,
                FunctionsProcessed = assignment.Functions.Count,
                ProcessingLogs = logs
            }, ct);
        }
        catch (Exception ex)
        {
            logs.Add($"❌ ERROR: {ex.Message}");
            _logger.LogError(ex, "💥 Error in DIRECT CUCS test");
            
            await Send.ResponseAsync(new DirectTestCucsResponse
            {
                Success = false,
                Message = $"Direct CUCS test failed: {ex.Message}",
                AssignmentId = string.Empty,
                FunctionsProcessed = 0,
                ProcessingLogs = logs
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
            Namespace = "Ensemble.Maestro.DirectTest",
            Description = $"Direct test class {codeUnitName} for CUCS mechanical dispatcher validation",
            ComplexityRating = 4,
            EstimatedMinutes = functionCount * 10,
            Priority = "High", // High priority for testing
            TargetLanguage = "CSharp"
        };

        // Create focused test functions
        for (int i = 1; i <= functionCount; i++)
        {
            var complexity = 3 + (i % 2); // Complexity 3-4 for simpler testing
            var isAsync = i % 2 == 0;
            
            var function = new FunctionAssignmentMessage
            {
                AssignmentId = Guid.NewGuid().ToString("N"),
                FunctionSpecificationId = Guid.NewGuid().ToString("N"),
                FunctionName = $"Calculate{codeUnitName}Value{i}",
                CodeUnit = codeUnitName,
                Signature = isAsync 
                    ? $"public async Task<decimal> Calculate{codeUnitName}Value{i}(decimal input)" 
                    : $"public decimal Calculate{codeUnitName}Value{i}(decimal input)",
                Description = $"Direct test function {i} - calculates value with complexity {complexity}",
                BusinessLogic = $"Perform mathematical calculation for test scenario {i}",
                ValidationRules = "Input must be positive decimal",
                ErrorHandling = "Return 0 for invalid inputs",
                SecurityConsiderations = "No security concerns for test function",
                ComplexityRating = complexity,
                EstimatedMinutes = complexity * 3,
                Priority = "High",
                TargetLanguage = "CSharp"
            };

            assignment.Functions.Add(function);
        }

        assignment.SimpleFunctionCount = assignment.Functions.Count(f => f.ComplexityRating <= 3);
        assignment.ComplexFunctionCount = assignment.Functions.Count(f => f.ComplexityRating > 3);

        return assignment;
    }
}