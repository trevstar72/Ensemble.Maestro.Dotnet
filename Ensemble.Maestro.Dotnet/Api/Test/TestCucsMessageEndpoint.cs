using FastEndpoints;
using Ensemble.Maestro.Dotnet.Core.Services;
using Ensemble.Maestro.Dotnet.Core.Data.Entities;
using Ensemble.Maestro.Dotnet.Core.Agents;

namespace Ensemble.Maestro.Dotnet.Api.Test;

public class TestCucsMessageRequest
{
    public string? TestId { get; set; }
}

public class TestCucsMessageResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int CodeUnitAssignmentsSent { get; set; }
    public List<string> Warnings { get; set; } = new();
    public string? Error { get; set; }
}

public class TestCucsMessageEndpoint : Endpoint<TestCucsMessageRequest, TestCucsMessageResponse>
{
    private readonly IDesignerOutputStorageService _designerOutputStorageService;
    private readonly ILogger<TestCucsMessageEndpoint> _logger;

    public TestCucsMessageEndpoint(
        IDesignerOutputStorageService designerOutputStorageService,
        ILogger<TestCucsMessageEndpoint> logger)
    {
        _designerOutputStorageService = designerOutputStorageService;
        _logger = logger;
    }

    public override void Configure()
    {
        Post("/api/test/cucs-message");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Test CUCS message generation directly";
            s.Description = "Tests the CodeUnitAssignment message generation and CUCS queue delivery bypassing LLM calls";
        });
    }

    public override async Task HandleAsync(TestCucsMessageRequest req, CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("🧪 CUCS MESSAGE TEST: Starting direct CUCS message generation test");

            // Create mock code units and function specs directly
            var mockCodeUnits = CreateMockCodeUnits();
            var mockFunctionSpecs = CreateMockFunctionSpecs();
            var mockContext = CreateMockContext();

            _logger.LogInformation("🧪 CUCS MESSAGE TEST: Created {CodeUnits} mock code units and {FunctionSpecs} mock function specs", 
                mockCodeUnits.Count, mockFunctionSpecs.Count);

            // Use reflection to call the private GenerateAndSendCodeUnitAssignmentsAsync method
            var method = typeof(DesignerOutputStorageService).GetMethod("GenerateAndSendCodeUnitAssignmentsAsync", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (method == null)
            {
                await Send.ResponseAsync(new TestCucsMessageResponse
                {
                    Success = false,
                    Message = "Could not find GenerateAndSendCodeUnitAssignmentsAsync method",
                    Error = "Reflection failed"
                }, 500, ct);
                return;
            }

            _logger.LogInformation("🧪 CUCS MESSAGE TEST: Calling GenerateAndSendCodeUnitAssignmentsAsync via reflection...");

            var result = await (Task<(int assignmentsSent, List<string> errors)>)method.Invoke(
                _designerOutputStorageService, 
                new object[] { mockCodeUnits, mockFunctionSpecs, mockContext, ct })!;

            _logger.LogInformation("🧪 CUCS MESSAGE TEST: Result - {AssignmentsSent} assignments sent, {Errors} errors", 
                result.assignmentsSent, result.errors.Count);

            await Send.OkAsync(new TestCucsMessageResponse
            {
                Success = result.errors.Count == 0,
                Message = result.errors.Count == 0 
                    ? $"CUCS message test completed successfully! {result.assignmentsSent} assignments sent"
                    : $"CUCS message test completed with errors: {string.Join(", ", result.errors)}",
                CodeUnitAssignmentsSent = result.assignmentsSent,
                Warnings = result.errors
            }, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "🧪 CUCS MESSAGE TEST: Failed with exception");
            await Send.ResponseAsync(new TestCucsMessageResponse
            {
                Success = false,
                Message = "CUCS message test failed with exception",
                Error = ex.Message
            }, 500, ct);
        }
    }

    private List<CodeUnit> CreateMockCodeUnits()
    {
        return new List<CodeUnit>
        {
            new CodeUnit
            {
                Id = Guid.NewGuid(),
                CrossReferenceId = Guid.NewGuid(),
                Name = "CalculatorService",
                UnitType = "Class",
                Language = "C#",
                Namespace = "Calculator",
                Description = "Main calculator service with arithmetic operations",
                FunctionCount = 4,
                Priority = "High",
                ComplexityRating = 3,
                EstimatedMinutes = 20,
                Status = "Planned",
                ProcessingStage = "Extracted",
                SimpleFunctionCount = 4,
                ComplexFunctionCount = 0,
                CompletionPercentage = 0,
                Version = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new CodeUnit
            {
                Id = Guid.NewGuid(),
                CrossReferenceId = Guid.NewGuid(),
                Name = "MathUtility",
                UnitType = "Class",
                Language = "C#",
                Namespace = "Math",
                Description = "Utility class for advanced mathematical operations",
                FunctionCount = 2,
                Priority = "Medium",
                ComplexityRating = 4,
                EstimatedMinutes = 20,
                Status = "Planned",
                ProcessingStage = "Extracted",
                SimpleFunctionCount = 0,
                ComplexFunctionCount = 2,
                CompletionPercentage = 0,
                Version = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };
    }

    private List<FunctionSpecification> CreateMockFunctionSpecs()
    {

        return new List<FunctionSpecification>
        {
            new FunctionSpecification
            {
                Id = Guid.NewGuid(),
                CrossReferenceId = Guid.NewGuid(),
                FunctionName = "Add",
                CodeUnit = "CalculatorService",
                Namespace = "Calculator",
                Signature = "Add(double a, double b): double",
                Description = "Performs addition of two numbers",
                ComplexityRating = 2,
                EstimatedMinutes = 5,
                Priority = "High",
                Status = "Created",
                Language = "C#",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Version = 1
            },
            new FunctionSpecification
            {
                Id = Guid.NewGuid(),
                CrossReferenceId = Guid.NewGuid(),
                FunctionName = "Subtract",
                CodeUnit = "CalculatorService",
                Namespace = "Calculator",
                Signature = "Subtract(double a, double b): double",
                Description = "Performs subtraction of two numbers",
                ComplexityRating = 2,
                EstimatedMinutes = 5,
                Priority = "High",
                Status = "Created",
                Language = "C#",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Version = 1
            },
            new FunctionSpecification
            {
                Id = Guid.NewGuid(),
                CrossReferenceId = Guid.NewGuid(),
                FunctionName = "Multiply",
                CodeUnit = "CalculatorService",
                Namespace = "Calculator",
                Signature = "Multiply(double a, double b): double",
                Description = "Performs multiplication of two numbers",
                ComplexityRating = 2,
                EstimatedMinutes = 5,
                Priority = "High",
                Status = "Created",
                Language = "C#",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Version = 1
            },
            new FunctionSpecification
            {
                Id = Guid.NewGuid(),
                CrossReferenceId = Guid.NewGuid(),
                FunctionName = "Divide",
                CodeUnit = "CalculatorService",
                Namespace = "Calculator",
                Signature = "Divide(double a, double b): double",
                Description = "Performs division of two numbers",
                ComplexityRating = 3,
                EstimatedMinutes = 5,
                Priority = "High",
                Status = "Created",
                Language = "C#",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Version = 1
            },
            new FunctionSpecification
            {
                Id = Guid.NewGuid(),
                CrossReferenceId = Guid.NewGuid(),
                FunctionName = "Power",
                CodeUnit = "MathUtility",
                Namespace = "Math",
                Signature = "Power(double baseValue, double exponent): double",
                Description = "Calculates power of a number",
                ComplexityRating = 4,
                EstimatedMinutes = 10,
                Priority = "Medium",
                Status = "Created",
                Language = "C#",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Version = 1
            },
            new FunctionSpecification
            {
                Id = Guid.NewGuid(),
                CrossReferenceId = Guid.NewGuid(),
                FunctionName = "SquareRoot",
                CodeUnit = "MathUtility",
                Namespace = "Math",
                Signature = "SquareRoot(double value): double",
                Description = "Calculates square root of a number",
                ComplexityRating = 3,
                EstimatedMinutes = 10,
                Priority = "Medium",
                Status = "Created",
                Language = "C#",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Version = 1
            }
        };
    }

    private AgentExecutionContext CreateMockContext()
    {
        return new AgentExecutionContext
        {
            ExecutionId = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            PipelineExecutionId = Guid.NewGuid(),
            StageExecutionId = Guid.NewGuid(),
            Stage = "Design",
            InputPrompt = "Test CUCS message generation",
            TargetLanguage = "C#",
            DeploymentTarget = "Cloud"
        };
    }
}