using FastEndpoints;
using Ensemble.Maestro.Dotnet.Core.Services;
using Ardalis.Result;
using Ardalis.Result.AspNetCore;

namespace Ensemble.Maestro.Dotnet.Api.Executions;

/// <summary>
/// API endpoint to start a test execution without UI
/// </summary>
public class StartTestExecutionEndpoint : Endpoint<StartTestExecutionRequest, StartTestExecutionResponse>
{
    private readonly TestbenchService _testbenchService;
    private readonly ILogger<StartTestExecutionEndpoint> _logger;

    public StartTestExecutionEndpoint(TestbenchService testbenchService, ILogger<StartTestExecutionEndpoint> logger)
    {
        _testbenchService = testbenchService;
        _logger = logger;
    }

    public override void Configure()
    {
        Post("/api/executions/start");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Start a new test execution";
            s.Description = "Starts a new pipeline execution for testing agents with real LLM integration";
            s.Responses[200] = "Test execution started successfully";
            s.Responses[400] = "Invalid request parameters";
            s.Responses[500] = "Internal server error";
        });
    }

    public override async Task HandleAsync(StartTestExecutionRequest req, CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Starting test execution via API: {ProjectName}", req.ProjectName);

            var config = new TestConfiguration(
                ProjectId: req.ProjectId ?? Guid.Empty,
                TargetLanguage: req.TargetLanguage,
                DeploymentTarget: req.DeploymentTarget,
                AgentPoolSize: req.AgentPoolSize ?? 5,
                EstimatedDurationSeconds: (req.MaxExecutionTimeMinutes ?? 10) * 60
            );

            var execution = await _testbenchService.StartTestExecution(config);

            await Send.OkAsync(new StartTestExecutionResponse
            {
                ExecutionId = execution.Id,
                ProjectId = execution.ProjectId,
                Status = execution.Status,
                Stage = execution.Stage,
                StartedAt = execution.StartedAt,
                EstimatedDurationSeconds = execution.EstimatedDurationSeconds ?? 0,
                TotalFunctions = execution.TotalFunctions ?? 0,
                Message = "Test execution started successfully. Use /api/executions/{id}/status to monitor progress."
            }, ct);

            _logger.LogInformation("Test execution {ExecutionId} started successfully", execution.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start test execution");
            ThrowError($"Failed to start test execution: {ex.Message}", 500);
        }
    }
}

/// <summary>
/// Request model for starting a test execution
/// </summary>
public class StartTestExecutionRequest
{
    /// <summary>
    /// Optional existing project ID. If not provided, a new project will be created.
    /// </summary>
    public Guid? ProjectId { get; set; }

    /// <summary>
    /// Name for the test project (required if ProjectId not provided)
    /// </summary>
    public string ProjectName { get; set; } = $"API Test {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}";

    /// <summary>
    /// Description of the test execution
    /// </summary>
    public string Description { get; set; } = "Automated test execution via API";

    /// <summary>
    /// Target programming language (e.g., "C#", "TypeScript", "Python")
    /// </summary>
    public string? TargetLanguage { get; set; } = "C#";

    /// <summary>
    /// Target deployment environment (e.g., "Azure", "AWS", "Docker")
    /// </summary>
    public string? DeploymentTarget { get; set; } = "Azure";

    /// <summary>
    /// Number of agents to use in the pool (1-10)
    /// </summary>
    public int? AgentPoolSize { get; set; } = 3;

    /// <summary>
    /// Maximum execution time in minutes before timeout
    /// </summary>
    public int? MaxExecutionTimeMinutes { get; set; } = 15;
}

/// <summary>
/// Response model for starting a test execution
/// </summary>
public class StartTestExecutionResponse
{
    /// <summary>
    /// Unique identifier for the execution
    /// </summary>
    public Guid ExecutionId { get; set; }

    /// <summary>
    /// Project identifier
    /// </summary>
    public Guid ProjectId { get; set; }

    /// <summary>
    /// Current execution status
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Current pipeline stage
    /// </summary>
    public string Stage { get; set; } = string.Empty;

    /// <summary>
    /// When the execution started
    /// </summary>
    public DateTime StartedAt { get; set; }

    /// <summary>
    /// Estimated duration in seconds
    /// </summary>
    public int? EstimatedDurationSeconds { get; set; }

    /// <summary>
    /// Total number of functions/agents to execute
    /// </summary>
    public int TotalFunctions { get; set; }

    /// <summary>
    /// Success message
    /// </summary>
    public string Message { get; set; } = string.Empty;
}