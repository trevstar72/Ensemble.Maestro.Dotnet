using FastEndpoints;
using Ensemble.Maestro.Dotnet.Core.Services;
using Ensemble.Maestro.Dotnet.Core.Data.Repositories;
using Ardalis.Result;
using Ardalis.Result.AspNetCore;

namespace Ensemble.Maestro.Dotnet.Api.Executions;

/// <summary>
/// API endpoint to get execution status and logs for monitoring
/// </summary>
public class GetExecutionStatusEndpoint : Endpoint<GetExecutionStatusRequest, GetExecutionStatusResponse>
{
    private readonly TestbenchService _testbenchService;
    private readonly IAgentExecutionRepository _agentExecutionRepository;
    private readonly ILogger<GetExecutionStatusEndpoint> _logger;

    public GetExecutionStatusEndpoint(
        TestbenchService testbenchService, 
        IAgentExecutionRepository agentExecutionRepository,
        ILogger<GetExecutionStatusEndpoint> logger)
    {
        _testbenchService = testbenchService;
        _agentExecutionRepository = agentExecutionRepository;
        _logger = logger;
    }

    public override void Configure()
    {
        Get("/api/executions/{id}/status");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Get execution status and logs";
            s.Description = "Retrieves current status, progress, and logs for a running execution";
            s.Responses[200] = "Execution status retrieved successfully";
            s.Responses[404] = "Execution not found";
            s.Responses[500] = "Internal server error";
        });
    }

    public override async Task HandleAsync(GetExecutionStatusRequest req, CancellationToken ct)
    {
        try
        {
            var execution = await _testbenchService.GetExecutionDetails(req.Id);
            if (execution == null)
            {
                await Send.NotFoundAsync(ct);
                return;
            }

            // Get recent agent executions with logs
            var agentDetailsResult = await _agentExecutionRepository.GetByPipelineExecutionIdAsync(req.Id);
            if (!agentDetailsResult.IsSuccess)
            {
                ThrowError("Failed to get agent executions", 500);
                return;
            }
            var agentDetails = agentDetailsResult.Value;

            var response = new GetExecutionStatusResponse
            {
                ExecutionId = execution.Id,
                ProjectId = execution.ProjectId,
                Status = execution.Status,
                Stage = execution.Stage,
                StartedAt = execution.StartedAt,
                CompletedAt = execution.CompletedAt,
                ProgressPercentage = execution.ProgressPercentage,
                TotalFunctions = execution.TotalFunctions ?? 0,
                CompletedFunctions = execution.CompletedFunctions,
                FailedFunctions = execution.FailedFunctions,
                ActualDurationSeconds = execution.ActualDurationSeconds,
                ErrorMessage = execution.ErrorMessage,
                
                // Real-time metrics
                Agents = agentDetails.Select(a => new AgentExecutionStatus
                {
                    AgentId = a.Id,
                    AgentName = a.AgentName,
                    AgentType = a.AgentType,
                    Status = a.Status,
                    StartedAt = a.StartedAt,
                    CompletedAt = a.CompletedAt,
                    DurationSeconds = a.DurationSeconds ?? 0,
                    InputTokens = a.InputTokens ?? 0,
                    OutputTokens = a.OutputTokens ?? 0,
                    TotalTokens = a.TotalTokens ?? 0,
                    ExecutionCost = a.ExecutionCost ?? 0,
                    QualityScore = a.QualityScore ?? 0,
                    ConfidenceScore = a.ConfidenceScore ?? 0,
                    ErrorMessage = a.ErrorMessage,
                    ModelUsed = a.ModelUsed,
                    // Get last few log entries
                    RecentLogs = GetRecentLogEntries(a.ExecutionLogs, 10)
                }).ToList()
            };

            // Calculate real-time totals
            response.TotalTokensUsed = response.Agents.Sum(a => a.TotalTokens);
            response.TotalCostIncurred = response.Agents.Sum(a => a.ExecutionCost);
            response.AverageQualityScore = response.Agents.Where(a => a.Status == "Completed").Average(a => (double?)a.QualityScore) ?? 0;

            await Send.OkAsync(response, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get execution status for {ExecutionId}", req.Id);
            ThrowError($"Failed to get execution status: {ex.Message}", 500);
        }
    }

    private List<string> GetRecentLogEntries(string? executionLogs, int count)
    {
        if (string.IsNullOrEmpty(executionLogs))
            return new List<string>();

        return executionLogs
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .TakeLast(count)
            .ToList();
    }
}

/// <summary>
/// Request model for getting execution status
/// </summary>
public class GetExecutionStatusRequest
{
    /// <summary>
    /// Execution ID to monitor
    /// </summary>
    public Guid Id { get; set; }
}

/// <summary>
/// Response model for execution status
/// </summary>
public class GetExecutionStatusResponse
{
    /// <summary>
    /// Execution identifier
    /// </summary>
    public Guid ExecutionId { get; set; }

    /// <summary>
    /// Project identifier
    /// </summary>
    public Guid ProjectId { get; set; }

    /// <summary>
    /// Current status (Pending, Running, Completed, Failed)
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Current pipeline stage (Planning, Designing, Swarming, Building, Validating)
    /// </summary>
    public string Stage { get; set; } = string.Empty;

    /// <summary>
    /// Execution start time
    /// </summary>
    public DateTime StartedAt { get; set; }

    /// <summary>
    /// Execution completion time (if finished)
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Progress percentage (0-100)
    /// </summary>
    public int ProgressPercentage { get; set; }

    /// <summary>
    /// Total number of functions to execute
    /// </summary>
    public int TotalFunctions { get; set; }

    /// <summary>
    /// Number of completed functions
    /// </summary>
    public int CompletedFunctions { get; set; }

    /// <summary>
    /// Number of failed functions
    /// </summary>
    public int FailedFunctions { get; set; }

    /// <summary>
    /// Actual execution duration in seconds
    /// </summary>
    public int? ActualDurationSeconds { get; set; }

    /// <summary>
    /// Error message if execution failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Real-time metrics - Total tokens used across all agents
    /// </summary>
    public int TotalTokensUsed { get; set; }

    /// <summary>
    /// Real-time metrics - Total cost incurred
    /// </summary>
    public decimal TotalCostIncurred { get; set; }

    /// <summary>
    /// Real-time metrics - Average quality score
    /// </summary>
    public double AverageQualityScore { get; set; }

    /// <summary>
    /// Detailed status of individual agents
    /// </summary>
    public List<AgentExecutionStatus> Agents { get; set; } = new();

    /// <summary>
    /// Whether execution is still active
    /// </summary>
    public bool IsActive => Status is "Pending" or "Running";

    /// <summary>
    /// Whether execution completed successfully
    /// </summary>
    public bool IsCompleted => Status == "Completed";

    /// <summary>
    /// Whether execution failed
    /// </summary>
    public bool IsFailed => Status is "Failed" or "Error";
}

/// <summary>
/// Status information for individual agent executions
/// </summary>
public class AgentExecutionStatus
{
    /// <summary>
    /// Agent execution ID
    /// </summary>
    public Guid AgentId { get; set; }

    /// <summary>
    /// Agent name
    /// </summary>
    public string AgentName { get; set; } = string.Empty;

    /// <summary>
    /// Agent type (Planner, Designer, etc.)
    /// </summary>
    public string AgentType { get; set; } = string.Empty;

    /// <summary>
    /// Agent execution status
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Agent start time
    /// </summary>
    public DateTime? StartedAt { get; set; }

    /// <summary>
    /// Agent completion time
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Agent execution duration in seconds
    /// </summary>
    public int? DurationSeconds { get; set; }

    /// <summary>
    /// Input tokens used by this agent
    /// </summary>
    public int InputTokens { get; set; }

    /// <summary>
    /// Output tokens generated by this agent
    /// </summary>
    public int OutputTokens { get; set; }

    /// <summary>
    /// Total tokens (input + output)
    /// </summary>
    public int TotalTokens { get; set; }

    /// <summary>
    /// Cost incurred by this agent
    /// </summary>
    public decimal ExecutionCost { get; set; }

    /// <summary>
    /// Quality score (0-100)
    /// </summary>
    public int QualityScore { get; set; }

    /// <summary>
    /// Confidence score (0-100)
    /// </summary>
    public int ConfidenceScore { get; set; }

    /// <summary>
    /// Error message if agent failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// LLM model used
    /// </summary>
    public string? ModelUsed { get; set; }

    /// <summary>
    /// Recent log entries from this agent
    /// </summary>
    public List<string> RecentLogs { get; set; } = new();
}