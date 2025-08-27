using FastEndpoints;
using Ensemble.Maestro.Dotnet.Core.Services;
using Ensemble.Maestro.Dotnet.Core.Data.Repositories;
using Ardalis.Result;
using Ardalis.Result.AspNetCore;

namespace Ensemble.Maestro.Dotnet.Api.Executions;

/// <summary>
/// API endpoint to get detailed execution logs
/// </summary>
public class GetExecutionLogsEndpoint : Endpoint<GetExecutionLogsRequest, GetExecutionLogsResponse>
{
    private readonly TestbenchService _testbenchService;
    private readonly IAgentExecutionRepository _agentExecutionRepository;
    private readonly ILogger<GetExecutionLogsEndpoint> _logger;

    public GetExecutionLogsEndpoint(
        TestbenchService testbenchService, 
        IAgentExecutionRepository agentExecutionRepository,
        ILogger<GetExecutionLogsEndpoint> logger)
    {
        _testbenchService = testbenchService;
        _agentExecutionRepository = agentExecutionRepository;
        _logger = logger;
    }

    public override void Configure()
    {
        Get("/api/executions/{id}/logs");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Get execution logs";
            s.Description = "Retrieves detailed logs from pipeline and agent executions for debugging and monitoring";
            s.Responses[200] = "Logs retrieved successfully";
            s.Responses[404] = "Execution not found";
            s.Responses[500] = "Internal server error";
        });
    }

    public override async Task HandleAsync(GetExecutionLogsRequest req, CancellationToken ct)
    {
        try
        {
            var execution = await _testbenchService.GetExecutionDetails(req.Id);
            if (execution == null)
            {
                await Send.NotFoundAsync(ct);
                return;
            }

            // Get agent executions with detailed logs
            var agentExecutionsResult = await _agentExecutionRepository.GetByPipelineExecutionIdAsync(req.Id);
            if (!agentExecutionsResult.IsSuccess)
            {
                ThrowError("Failed to get agent executions", 500);
                return;
            }
            var agentExecutions = agentExecutionsResult.Value;

            var response = new GetExecutionLogsResponse
            {
                ExecutionId = execution.Id,
                ProjectId = execution.ProjectId,
                Status = execution.Status,
                Stage = execution.Stage,
                
                // Pipeline-level logs
                PipelineLogs = ParseLogEntries(execution.ExecutionLogs),
                
                // Agent-level logs with LLM interaction details
                AgentLogs = agentExecutions.Select(a => new AgentLogEntry
                {
                    AgentId = a.Id,
                    AgentName = a.AgentName,
                    AgentType = a.AgentType,
                    Status = a.Status,
                    StartedAt = a.StartedAt,
                    CompletedAt = a.CompletedAt,
                    InputPrompt = TruncateText(a.InputPrompt, req.MaxPromptLength ?? 500),
                    OutputResponse = TruncateText(a.OutputResponse, req.MaxResponseLength ?? 1000),
                    ExecutionLogs = ParseLogEntries(a.ExecutionLogs),
                    
                    // LLM metrics
                    InputTokens = a.InputTokens ?? 0,
                    OutputTokens = a.OutputTokens ?? 0,
                    TotalTokens = a.TotalTokens ?? 0,
                    ExecutionCost = a.ExecutionCost ?? 0,
                    ModelUsed = a.ModelUsed,
                    Temperature = a.Temperature,
                    
                    // Quality metrics  
                    QualityScore = a.QualityScore ?? 0,
                    ConfidenceScore = a.ConfidenceScore ?? 0,
                    
                    ErrorMessage = a.ErrorMessage,
                    ErrorStackTrace = req.IncludeStackTraces ? a.ErrorStackTrace : null
                }).ToList()
            };

            // Sort agents by execution order
            response.AgentLogs = response.AgentLogs.OrderBy(a => a.StartedAt).ToList();

            await Send.OkAsync(response, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get execution logs for {ExecutionId}", req.Id);
            ThrowError($"Failed to get execution logs: {ex.Message}", 500);
        }
    }

    private List<LogEntry> ParseLogEntries(string? logs)
    {
        if (string.IsNullOrEmpty(logs))
            return new List<LogEntry>();

        return logs.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(line => ParseLogLine(line))
            .Where(entry => entry != null)
            .ToList()!;
    }

    private LogEntry? ParseLogLine(string line)
    {
        try
        {
            // Simple log parsing - can be enhanced for structured logging
            var parts = line.Split(' ', 3);
            if (parts.Length >= 3)
            {
                return new LogEntry
                {
                    Timestamp = DateTime.TryParse($"{parts[0]} {parts[1]}", out var dt) ? dt : DateTime.UtcNow,
                    Level = ExtractLogLevel(line),
                    Message = parts[2]
                };
            }
            
            return new LogEntry
            {
                Timestamp = DateTime.UtcNow,
                Level = "INFO",
                Message = line
            };
        }
        catch
        {
            return new LogEntry
            {
                Timestamp = DateTime.UtcNow,
                Level = "INFO", 
                Message = line
            };
        }
    }

    private string ExtractLogLevel(string line)
    {
        var upperLine = line.ToUpper();
        if (upperLine.Contains("ERROR")) return "ERROR";
        if (upperLine.Contains("WARN")) return "WARN";
        if (upperLine.Contains("INFO")) return "INFO";
        if (upperLine.Contains("DEBUG")) return "DEBUG";
        return "INFO";
    }

    private string? TruncateText(string? text, int maxLength)
    {
        if (string.IsNullOrEmpty(text))
            return text;
            
        if (text.Length <= maxLength)
            return text;
            
        return text.Substring(0, maxLength) + "... [truncated]";
    }
}

/// <summary>
/// Request model for getting execution logs
/// </summary>
public class GetExecutionLogsRequest
{
    /// <summary>
    /// Execution ID to get logs for
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Maximum length for input prompts (default: 500)
    /// </summary>
    public int? MaxPromptLength { get; set; }

    /// <summary>
    /// Maximum length for output responses (default: 1000)
    /// </summary>
    public int? MaxResponseLength { get; set; }

    /// <summary>
    /// Include full stack traces in error logs (default: false)
    /// </summary>
    public bool IncludeStackTraces { get; set; } = false;
}

/// <summary>
/// Response model for execution logs
/// </summary>
public class GetExecutionLogsResponse
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
    /// Current execution status
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Current pipeline stage
    /// </summary>
    public string Stage { get; set; } = string.Empty;

    /// <summary>
    /// Pipeline-level logs
    /// </summary>
    public List<LogEntry> PipelineLogs { get; set; } = new();

    /// <summary>
    /// Detailed logs from each agent execution
    /// </summary>
    public List<AgentLogEntry> AgentLogs { get; set; } = new();
}

/// <summary>
/// Individual log entry
/// </summary>
public class LogEntry
{
    /// <summary>
    /// Log timestamp
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Log level (ERROR, WARN, INFO, DEBUG)
    /// </summary>
    public string Level { get; set; } = string.Empty;

    /// <summary>
    /// Log message
    /// </summary>
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Detailed agent execution log entry
/// </summary>
public class AgentLogEntry
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
    /// Agent type
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
    /// Input prompt sent to LLM (truncated if long)
    /// </summary>
    public string? InputPrompt { get; set; }

    /// <summary>
    /// Response received from LLM (truncated if long)
    /// </summary>
    public string? OutputResponse { get; set; }

    /// <summary>
    /// Agent execution logs
    /// </summary>
    public List<LogEntry> ExecutionLogs { get; set; } = new();

    /// <summary>
    /// LLM input tokens used
    /// </summary>
    public int InputTokens { get; set; }

    /// <summary>
    /// LLM output tokens generated
    /// </summary>
    public int OutputTokens { get; set; }

    /// <summary>
    /// Total tokens (input + output)
    /// </summary>
    public int TotalTokens { get; set; }

    /// <summary>
    /// Cost incurred for this agent execution
    /// </summary>
    public decimal ExecutionCost { get; set; }

    /// <summary>
    /// LLM model used
    /// </summary>
    public string? ModelUsed { get; set; }

    /// <summary>
    /// LLM temperature parameter
    /// </summary>
    public float? Temperature { get; set; }

    /// <summary>
    /// Quality score (0-100)
    /// </summary>
    public int QualityScore { get; set; }

    /// <summary>
    /// Confidence score (0-100) 
    /// </summary>
    public int ConfidenceScore { get; set; }

    /// <summary>
    /// Error message if execution failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Full stack trace if requested and available
    /// </summary>
    public string? ErrorStackTrace { get; set; }
}