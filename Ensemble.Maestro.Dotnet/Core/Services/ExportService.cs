using System.Text;
using System.Text.Json;
using Ensemble.Maestro.Dotnet.Core.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Ensemble.Maestro.Dotnet.Core.Services;

public class ExportService
{
    private readonly TestbenchService _testbenchService;
    private readonly ILogger<ExportService> _logger;

    public ExportService(TestbenchService testbenchService, ILogger<ExportService> logger)
    {
        _testbenchService = testbenchService;
        _logger = logger;
    }

    public async Task<ExportResult> ExportPipelineExecution(Guid executionId, ExportFormat format = ExportFormat.Json)
    {
        try
        {
            var execution = await _testbenchService.GetExecutionDetails(executionId);
            if (execution == null)
            {
                return ExportResult.CreateError("Pipeline execution not found");
            }

            var agentExecutions = await _testbenchService.GetAgentExecutionsByPipeline(executionId);

            return format switch
            {
                ExportFormat.Json => await ExportToJson(execution, agentExecutions),
                ExportFormat.Csv => await ExportToCsv(execution, agentExecutions),
                ExportFormat.Excel => await ExportToExcel(execution, agentExecutions),
                _ => ExportResult.CreateError("Unsupported export format")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export pipeline execution {ExecutionId}", executionId);
            return ExportResult.CreateError($"Export failed: {ex.Message}");
        }
    }

    public async Task<ExportResult> ExportTestbenchSummary(DateTime? fromDate = null, DateTime? toDate = null, ExportFormat format = ExportFormat.Json)
    {
        try
        {
            var executions = await _testbenchService.GetRecentExecutions(1000);
            
            if (fromDate.HasValue || toDate.HasValue)
            {
                executions = executions.Where(e => 
                    (!fromDate.HasValue || e.StartedAt >= fromDate.Value) &&
                    (!toDate.HasValue || e.StartedAt <= toDate.Value)
                ).ToList();
            }

            var stats = await _testbenchService.GetTestbenchStats();

            return format switch
            {
                ExportFormat.Json => await ExportSummaryToJson(executions, stats),
                ExportFormat.Csv => await ExportSummaryToCsv(executions, stats),
                ExportFormat.Excel => await ExportSummaryToExcel(executions, stats),
                _ => ExportResult.CreateError("Unsupported export format")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export testbench summary");
            return ExportResult.CreateError($"Export failed: {ex.Message}");
        }
    }

    private async Task<ExportResult> ExportToJson(PipelineExecution execution, List<AgentExecution> agentExecutions)
    {
        var exportData = new
        {
            ExecutionDetails = new
            {
                Id = execution.Id,
                ProjectId = execution.ProjectId,
                ProjectName = execution.Project?.Name,
                Status = execution.Status,
                Stage = execution.Stage,
                StartedAt = execution.StartedAt,
                CompletedAt = execution.CompletedAt,
                Duration = execution.ActualDurationSeconds,
                Progress = execution.ProgressPercentage,
                TargetLanguage = execution.TargetLanguage,
                DeploymentTarget = execution.DeploymentTarget,
                AgentPoolSize = execution.AgentPoolSize,
                TotalFunctions = execution.TotalFunctions,
                CompletedFunctions = execution.CompletedFunctions,
                FailedFunctions = execution.FailedFunctions,
                ErrorMessage = execution.ErrorMessage
            },
            StageExecutions = execution.StageExecutions.Select(s => new
            {
                Id = s.Id,
                StageName = s.StageName,
                Status = s.Status,
                ExecutionOrder = s.ExecutionOrder,
                StartedAt = s.StartedAt,
                CompletedAt = s.CompletedAt,
                Duration = s.DurationSeconds,
                ItemsProcessed = s.ItemsProcessed,
                ItemsCompleted = s.ItemsCompleted,
                ItemsFailed = s.ItemsFailed,
                ProgressPercentage = s.ProgressPercentage,
                ErrorMessage = s.ErrorMessage
            }).OrderBy(s => s.ExecutionOrder),
            AgentExecutions = agentExecutions.Select(a => new
            {
                Id = a.Id,
                AgentName = a.AgentName,
                AgentType = a.AgentType,
                AgentSpecialization = a.AgentSpecialization,
                Status = a.Status,
                Priority = a.Priority,
                StartedAt = a.StartedAt,
                CompletedAt = a.CompletedAt,
                Duration = a.DurationSeconds,
                InputTokens = a.InputTokens,
                OutputTokens = a.OutputTokens,
                TotalTokens = a.TotalTokens,
                ExecutionCost = a.ExecutionCost,
                ModelUsed = a.ModelUsed,
                Temperature = a.Temperature,
                MaxTokens = a.MaxTokens,
                QualityScore = a.QualityScore,
                ConfidenceScore = a.ConfidenceScore,
                RetryAttempt = a.RetryAttempt,
                ErrorMessage = a.ErrorMessage,
                InputPrompt = a.InputPrompt,
                OutputResponse = a.OutputResponse
            }).OrderBy(a => a.StartedAt),
            ExportedAt = DateTime.UtcNow,
            ExportVersion = "1.0"
        };

        var json = JsonSerializer.Serialize(exportData, new JsonSerializerOptions 
        { 
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        var filename = $"pipeline_execution_{execution.Id:N}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.json";
        return ExportResult.CreateSuccess(Encoding.UTF8.GetBytes(json), filename, "application/json");
    }

    private async Task<ExportResult> ExportToCsv(PipelineExecution execution, List<AgentExecution> agentExecutions)
    {
        var csv = new StringBuilder();
        
        // Pipeline execution summary
        csv.AppendLine("PIPELINE EXECUTION SUMMARY");
        csv.AppendLine($"Execution ID,{execution.Id}");
        csv.AppendLine($"Project,{execution.Project?.Name ?? "N/A"}");
        csv.AppendLine($"Status,{execution.Status}");
        csv.AppendLine($"Stage,{execution.Stage}");
        csv.AppendLine($"Started At,{execution.StartedAt:yyyy-MM-dd HH:mm:ss} UTC");
        csv.AppendLine($"Completed At,{execution.CompletedAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? "N/A"} UTC");
        csv.AppendLine($"Duration (seconds),{execution.ActualDurationSeconds ?? 0}");
        csv.AppendLine($"Progress (%),{execution.ProgressPercentage}");
        csv.AppendLine($"Target Language,{execution.TargetLanguage ?? "N/A"}");
        csv.AppendLine($"Agent Pool Size,{execution.AgentPoolSize ?? 0}");
        csv.AppendLine();

        // Agent executions
        csv.AppendLine("AGENT EXECUTIONS");
        csv.AppendLine("Agent Name,Agent Type,Status,Started At,Completed At,Duration (sec),Input Tokens,Output Tokens,Total Tokens,Cost ($),Model,Quality Score,Confidence Score,Error");
        
        foreach (var agent in agentExecutions.OrderBy(a => a.StartedAt))
        {
            csv.AppendLine($"\"{agent.AgentName}\"," +
                          $"\"{agent.AgentType}\"," +
                          $"\"{agent.Status}\"," +
                          $"\"{agent.StartedAt:yyyy-MM-dd HH:mm:ss}\"," +
                          $"\"{agent.CompletedAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? "N/A"}\"," +
                          $"{agent.DurationSeconds ?? 0}," +
                          $"{agent.InputTokens ?? 0}," +
                          $"{agent.OutputTokens ?? 0}," +
                          $"{agent.TotalTokens ?? 0}," +
                          $"{agent.ExecutionCost ?? 0:F4}," +
                          $"\"{agent.ModelUsed ?? "N/A"}\"," +
                          $"{agent.QualityScore ?? 0}," +
                          $"{agent.ConfidenceScore ?? 0}," +
                          $"\"{agent.ErrorMessage?.Replace("\"", "\"\"") ?? ""}\""
            );
        }

        var filename = $"pipeline_execution_{execution.Id:N}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";
        return ExportResult.CreateSuccess(Encoding.UTF8.GetBytes(csv.ToString()), filename, "text/csv");
    }

    private async Task<ExportResult> ExportToExcel(PipelineExecution execution, List<AgentExecution> agentExecutions)
    {
        // For now, return CSV format with Excel MIME type
        // In a real implementation, you would use a library like EPPlus or ClosedXML
        var csvResult = await ExportToCsv(execution, agentExecutions);
        
        if (csvResult.Success)
        {
            var filename = csvResult.Filename.Replace(".csv", ".xlsx");
            return ExportResult.CreateSuccess(csvResult.Data, filename, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        }
        
        return csvResult;
    }

    private async Task<ExportResult> ExportSummaryToJson(List<PipelineExecution> executions, TestbenchStats stats)
    {
        var exportData = new
        {
            Summary = new
            {
                TotalExecutions = stats.TotalExecutions,
                ActiveExecutions = stats.ActiveExecutions,
                CompletedExecutions = stats.CompletedExecutions,
                FailedExecutions = stats.FailedExecutions,
                SuccessRate = stats.SuccessRate,
                TotalAgentExecutions = stats.TotalAgentExecutions,
                TotalTokensConsumed = stats.TotalTokensConsumed,
                TotalCostUSD = stats.TotalCostUSD,
                AverageExecutionTimeSeconds = stats.AverageExecutionTimeSeconds
            },
            Executions = executions.Select(e => new
            {
                Id = e.Id,
                ProjectName = e.Project?.Name,
                Status = e.Status,
                Stage = e.Stage,
                StartedAt = e.StartedAt,
                CompletedAt = e.CompletedAt,
                Duration = e.ActualDurationSeconds,
                AgentCount = e.AgentExecutions.Count,
                TotalTokens = e.AgentExecutions.Sum(a => a.TotalTokens ?? 0),
                TotalCost = e.AgentExecutions.Sum(a => a.ExecutionCost ?? 0),
                SuccessRate = e.CompletedFunctions > 0 ? (double)e.CompletedFunctions / (e.CompletedFunctions + e.FailedFunctions) * 100 : 0
            }).OrderByDescending(e => e.StartedAt),
            ExportedAt = DateTime.UtcNow,
            ExportVersion = "1.0"
        };

        var json = JsonSerializer.Serialize(exportData, new JsonSerializerOptions 
        { 
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        var filename = $"testbench_summary_{DateTime.UtcNow:yyyyMMdd_HHmmss}.json";
        return ExportResult.CreateSuccess(Encoding.UTF8.GetBytes(json), filename, "application/json");
    }

    private async Task<ExportResult> ExportSummaryToCsv(List<PipelineExecution> executions, TestbenchStats stats)
    {
        var csv = new StringBuilder();
        
        // Summary statistics
        csv.AppendLine("TESTBENCH SUMMARY STATISTICS");
        csv.AppendLine($"Total Executions,{stats.TotalExecutions}");
        csv.AppendLine($"Active Executions,{stats.ActiveExecutions}");
        csv.AppendLine($"Completed Executions,{stats.CompletedExecutions}");
        csv.AppendLine($"Failed Executions,{stats.FailedExecutions}");
        csv.AppendLine($"Success Rate (%),{stats.SuccessRate:F1}");
        csv.AppendLine($"Total Agent Executions,{stats.TotalAgentExecutions}");
        csv.AppendLine($"Total Tokens Consumed,{stats.TotalTokensConsumed}");
        csv.AppendLine($"Total Cost (USD),{stats.TotalCostUSD:F4}");
        csv.AppendLine($"Average Execution Time (sec),{stats.AverageExecutionTimeSeconds:F1}");
        csv.AppendLine();

        // Execution details
        csv.AppendLine("EXECUTION DETAILS");
        csv.AppendLine("Execution ID,Project,Status,Stage,Started At,Completed At,Duration (sec),Agent Count,Total Tokens,Total Cost ($)");
        
        foreach (var execution in executions.OrderByDescending(e => e.StartedAt))
        {
            csv.AppendLine($"{execution.Id}," +
                          $"\"{execution.Project?.Name ?? "N/A"}\"," +
                          $"\"{execution.Status}\"," +
                          $"\"{execution.Stage}\"," +
                          $"\"{execution.StartedAt:yyyy-MM-dd HH:mm:ss}\"," +
                          $"\"{execution.CompletedAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? "N/A"}\"," +
                          $"{execution.ActualDurationSeconds ?? 0}," +
                          $"{execution.AgentExecutions.Count}," +
                          $"{execution.AgentExecutions.Sum(a => a.TotalTokens ?? 0)}," +
                          $"{execution.AgentExecutions.Sum(a => a.ExecutionCost ?? 0):F4}"
            );
        }

        var filename = $"testbench_summary_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";
        return ExportResult.CreateSuccess(Encoding.UTF8.GetBytes(csv.ToString()), filename, "text/csv");
    }

    private async Task<ExportResult> ExportSummaryToExcel(List<PipelineExecution> executions, TestbenchStats stats)
    {
        // For now, return CSV format with Excel MIME type
        var csvResult = await ExportSummaryToCsv(executions, stats);
        
        if (csvResult.Success)
        {
            var filename = csvResult.Filename.Replace(".csv", ".xlsx");
            return ExportResult.CreateSuccess(csvResult.Data, filename, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        }
        
        return csvResult;
    }
}

public enum ExportFormat
{
    Json,
    Csv,
    Excel
}

public class ExportResult
{
    public bool Success { get; init; }
    public byte[] Data { get; init; } = Array.Empty<byte>();
    public string Filename { get; init; } = "";
    public string ContentType { get; init; } = "";
    public string ErrorMessage { get; init; } = "";

    public static ExportResult CreateSuccess(byte[] data, string filename, string contentType)
    {
        return new ExportResult 
        { 
            Success = true, 
            Data = data, 
            Filename = filename, 
            ContentType = contentType 
        };
    }

    public static ExportResult CreateError(string errorMessage)
    {
        return new ExportResult 
        { 
            Success = false, 
            ErrorMessage = errorMessage 
        };
    }
}