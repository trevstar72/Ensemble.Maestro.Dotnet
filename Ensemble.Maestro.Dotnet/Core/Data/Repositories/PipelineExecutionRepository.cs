using Ensemble.Maestro.Dotnet.Core.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Ardalis.Result;

namespace Ensemble.Maestro.Dotnet.Core.Data.Repositories;

/// <summary>
/// Repository implementation for PipelineExecution entities with specialized operations
/// </summary>
public class PipelineExecutionRepository : Repository<PipelineExecution>, IPipelineExecutionRepository
{
    public PipelineExecutionRepository(MaestroDbContext context) : base(context)
    {
    }

    // Query operations
    public async Task<Result<IEnumerable<PipelineExecution>>> GetByProjectIdAsync(Guid projectId)
    {
        try
        {
            var executions = await _dbSet
                .Where(pe => pe.ProjectId == projectId)
                .OrderByDescending(pe => pe.StartedAt)
                .ToListAsync();
            
            return Result.Success(executions.AsEnumerable());
        }
        catch (Exception ex)
        {
            return Result.Error(new ErrorList(new[] { $"Error retrieving pipeline executions for project {projectId}: {ex.Message}" }, null));
        }
    }

    public async Task<Result<IEnumerable<PipelineExecution>>> GetByStatusAsync(string status)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(status))
                return Result.Invalid(new ValidationError { ErrorMessage = "Status cannot be null or empty" });

            var executions = await _dbSet
                .Where(pe => pe.Status == status)
                .OrderByDescending(pe => pe.StartedAt)
                .ToListAsync();
            
            return Result.Success(executions.AsEnumerable());
        }
        catch (Exception ex)
        {
            return Result.Error(new ErrorList(new[] { $"Error retrieving pipeline executions by status '{status}': {ex.Message}" }, null));
        }
    }

    public async Task<Result<IEnumerable<PipelineExecution>>> GetActiveExecutionsAsync()
    {
        try
        {
            var executions = await _dbSet
                .Where(pe => pe.Status == "Running" || pe.Status == "InProgress" || pe.Status == "Queued")
                .OrderByDescending(pe => pe.StartedAt)
                .ToListAsync();
            
            return Result.Success(executions.AsEnumerable());
        }
        catch (Exception ex)
        {
            return Result.Error(new ErrorList(new[] { $"Error retrieving active pipeline executions: {ex.Message}" }, null));
        }
    }

    public async Task<Result<IEnumerable<PipelineExecution>>> GetCompletedExecutionsAsync()
    {
        try
        {
            var executions = await _dbSet
                .Where(pe => pe.Status == "Completed")
                .OrderByDescending(pe => pe.CompletedAt)
                .ToListAsync();
            
            return Result.Success(executions.AsEnumerable());
        }
        catch (Exception ex)
        {
            return Result.Error(new ErrorList(new[] { $"Error retrieving completed pipeline executions: {ex.Message}" }, null));
        }
    }

    public async Task<Result<IEnumerable<PipelineExecution>>> GetFailedExecutionsAsync()
    {
        try
        {
            var executions = await _dbSet
                .Where(pe => pe.Status == "Failed" || pe.Status == "Error")
                .OrderByDescending(pe => pe.StartedAt)
                .ToListAsync();
            
            return Result.Success(executions.AsEnumerable());
        }
        catch (Exception ex)
        {
            return Result.Error(new ErrorList(new[] { $"Error retrieving failed pipeline executions: {ex.Message}" }, null));
        }
    }

    public async Task<Result<PipelineExecution>> GetLatestForProjectAsync(Guid projectId)
    {
        try
        {
            var execution = await _dbSet
                .Where(pe => pe.ProjectId == projectId)
                .OrderByDescending(pe => pe.StartedAt)
                .FirstOrDefaultAsync();
            
            return execution != null 
                ? Result.Success(execution)
                : Result.NotFound($"No pipeline executions found for project {projectId}");
        }
        catch (Exception ex)
        {
            return Result.Error(new ErrorList(new[] { $"Error retrieving latest pipeline execution for project {projectId}: {ex.Message}" }, null));
        }
    }

    // Complex queries with related data
    public async Task<Result<PipelineExecution>> GetWithAllRelatedDataAsync(Guid id)
    {
        try
        {
            var execution = await _dbSet
                .Include(pe => pe.StageExecutions.OrderBy(se => se.ExecutionOrder))
                    .ThenInclude(se => se.AgentExecutions)
                        .ThenInclude(ae => ae.Messages)
                .Include(pe => pe.OrchestrationResults)
                .Include(pe => pe.AgentExecutions)
                    .ThenInclude(ae => ae.Messages)
                .Include(pe => pe.Project)
                .FirstOrDefaultAsync(pe => pe.Id == id);
            
            return execution != null 
                ? Result.Success(execution)
                : Result.NotFound($"Pipeline execution with ID {id} not found");
        }
        catch (Exception ex)
        {
            return Result.Error(new ErrorList(new[] { $"Error retrieving pipeline execution with all related data for ID {id}: {ex.Message}" }, null));
        }
    }

    public async Task<Result<PipelineExecution>> GetWithStageExecutionsAsync(Guid id)
    {
        try
        {
            var execution = await _dbSet
                .Include(pe => pe.StageExecutions.OrderBy(se => se.ExecutionOrder))
                    .ThenInclude(se => se.AgentExecutions)
                .FirstOrDefaultAsync(pe => pe.Id == id);
            
            return execution != null 
                ? Result.Success(execution)
                : Result.NotFound($"Pipeline execution with ID {id} not found");
        }
        catch (Exception ex)
        {
            return Result.Error(new ErrorList(new[] { $"Error retrieving pipeline execution with stage executions for ID {id}: {ex.Message}" }, null));
        }
    }

    public async Task<Result<PipelineExecution>> GetWithOrchestrationResultsAsync(Guid id)
    {
        try
        {
            var execution = await _dbSet
                .Include(pe => pe.OrchestrationResults.OrderByDescending(or => or.StartedAt))
                .FirstOrDefaultAsync(pe => pe.Id == id);
            
            return execution != null 
                ? Result.Success(execution)
                : Result.NotFound($"Pipeline execution with ID {id} not found");
        }
        catch (Exception ex)
        {
            return Result.Error(new ErrorList(new[] { $"Error retrieving pipeline execution with orchestration results for ID {id}: {ex.Message}" }, null));
        }
    }

    public async Task<Result<PipelineExecution>> GetWithAgentExecutionsAsync(Guid id)
    {
        try
        {
            var execution = await _dbSet
                .Include(pe => pe.AgentExecutions)
                    .ThenInclude(ae => ae.Messages)
                .FirstOrDefaultAsync(pe => pe.Id == id);
            
            return execution != null 
                ? Result.Success(execution)
                : Result.NotFound($"Pipeline execution with ID {id} not found");
        }
        catch (Exception ex)
        {
            return Result.Error(new ErrorList(new[] { $"Error retrieving pipeline execution with agent executions for ID {id}: {ex.Message}" }, null));
        }
    }

    public async Task<Result<IEnumerable<PipelineExecution>>> GetExecutionsWithStagesAsync(Guid projectId)
    {
        try
        {
            var executions = await _dbSet
                .Where(pe => pe.ProjectId == projectId)
                .Include(pe => pe.StageExecutions.OrderBy(se => se.ExecutionOrder))
                .OrderByDescending(pe => pe.StartedAt)
                .ToListAsync();
            
            return Result.Success(executions.AsEnumerable());
        }
        catch (Exception ex)
        {
            return Result.Error(new ErrorList(new[] { $"Error retrieving pipeline executions with stages for project {projectId}: {ex.Message}" }, null));
        }
    }

    // Analytics and statistics
    public async Task<Result<int>> GetExecutionCountByStatusAsync(string status)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(status))
                return Result.Invalid(new ValidationError { ErrorMessage = "Status cannot be null or empty" });

            var count = await _dbSet.CountAsync(pe => pe.Status == status);
            return Result.Success(count);
        }
        catch (Exception ex)
        {
            return Result.Error(new ErrorList(new[] { $"Error counting pipeline executions by status '{status}': {ex.Message}" }, null));
        }
    }

    public async Task<Result<decimal>> GetAverageExecutionTimeAsync()
    {
        try
        {
            var completedExecutions = await _dbSet
                .Where(pe => pe.Status == "Completed" && pe.CompletedAt.HasValue)
                .Select(pe => new { pe.StartedAt, pe.CompletedAt })
                .ToListAsync();

            if (!completedExecutions.Any()) return Result.Success(0m);

            var durations = completedExecutions
                .Where(e => e.CompletedAt.HasValue)
                .Select(e => (decimal)(e.CompletedAt!.Value - e.StartedAt).TotalMinutes)
                .ToList();

            var average = durations.Any() ? durations.Average() : 0;
            return Result.Success(average);
        }
        catch (Exception ex)
        {
            return Result.Error(new ErrorList(new[] { $"Error calculating average execution time: {ex.Message}" }, null));
        }
    }

    public async Task<Result<(int Total, int Running, int Completed, int Failed)>> GetExecutionStatisticsAsync()
    {
        try
        {
            var executions = await _dbSet.ToListAsync();
            var total = executions.Count;
            var running = executions.Count(pe => pe.Status == "Running" || pe.Status == "InProgress" || pe.Status == "Queued");
            var completed = executions.Count(pe => pe.Status == "Completed");
            var failed = executions.Count(pe => pe.Status == "Failed" || pe.Status == "Error");

            return Result.Success((total, running, completed, failed));
        }
        catch (Exception ex)
        {
            return Result.Error(new ErrorList(new[] { $"Error retrieving execution statistics: {ex.Message}" }, null));
        }
    }

    public async Task<Result<(int Total, int Running, int Completed, int Failed)>> GetExecutionStatisticsForProjectAsync(Guid projectId)
    {
        try
        {
            var executions = await _dbSet.Where(pe => pe.ProjectId == projectId).ToListAsync();
            var total = executions.Count;
            var running = executions.Count(pe => pe.Status == "Running" || pe.Status == "InProgress" || pe.Status == "Queued");
            var completed = executions.Count(pe => pe.Status == "Completed");
            var failed = executions.Count(pe => pe.Status == "Failed" || pe.Status == "Error");

            return Result.Success((total, running, completed, failed));
        }
        catch (Exception ex)
        {
            return Result.Error(new ErrorList(new[] { $"Error retrieving execution statistics for project {projectId}: {ex.Message}" }, null));
        }
    }

    public async Task<Result<IEnumerable<(string Status, int Count)>>> GetStatusDistributionAsync()
    {
        try
        {
            var distribution = await _dbSet
                .GroupBy(pe => pe.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Select(x => ValueTuple.Create(x.Status, x.Count))
                .ToListAsync();
            
            return Result.Success(distribution.AsEnumerable());
        }
        catch (Exception ex)
        {
            return Result.Error(new ErrorList(new[] { $"Error retrieving status distribution: {ex.Message}" }, null));
        }
    }

    public async Task<Result<IEnumerable<(DateTime Date, int Count)>>> GetExecutionTrendAsync(int days = 30)
    {
        try
        {
            if (days <= 0)
                return Result.Invalid(new ValidationError { ErrorMessage = "Days must be greater than zero" });

            var startDate = DateTime.UtcNow.AddDays(-days).Date;
            var trend = await _dbSet
                .Where(pe => pe.StartedAt >= startDate)
                .GroupBy(pe => pe.StartedAt.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .OrderBy(x => x.Date)
                .Select(x => ValueTuple.Create(x.Date, x.Count))
                .ToListAsync();
            
            return Result.Success(trend.AsEnumerable());
        }
        catch (Exception ex)
        {
            return Result.Error(new ErrorList(new[] { $"Error retrieving execution trend for {days} days: {ex.Message}" }, null));
        }
    }

    // Performance metrics
    public async Task<Result<decimal>> GetAverageTokenUsageAsync()
    {
        try
        {
            // Calculate token usage from related AgentExecutions since PipelineExecution doesn't have TotalTokens
            var executions = await _dbSet
                .Include(pe => pe.AgentExecutions)
                .ToListAsync();

            var tokenTotals = executions
                .Select(pe => pe.AgentExecutions
                    .Where(ae => ae.InputTokens.HasValue && ae.OutputTokens.HasValue)
                    .Sum(ae => ae.InputTokens!.Value + ae.OutputTokens!.Value))
                .Where(total => total > 0)
                .ToList();

            var average = tokenTotals.Any() ? (decimal)tokenTotals.Average() : 0;
            return Result.Success(average);
        }
        catch (Exception ex)
        {
            return Result.Error(new ErrorList(new[] { $"Error calculating average token usage: {ex.Message}" }, null));
        }
    }

    public async Task<Result<decimal>> GetAverageCostAsync()
    {
        try
        {
            // Calculate cost from related AgentExecutions since PipelineExecution doesn't have TotalCost
            var executions = await _dbSet
                .Include(pe => pe.AgentExecutions)
                .ToListAsync();

            var costTotals = executions
                .Select(pe => pe.AgentExecutions
                    .Where(ae => ae.ExecutionCost.HasValue)
                    .Sum(ae => ae.ExecutionCost!.Value))
                .Where(total => total > 0)
                .ToList();

            var average = costTotals.Any() ? costTotals.Average() : 0;
            return Result.Success(average);
        }
        catch (Exception ex)
        {
            return Result.Error(new ErrorList(new[] { $"Error calculating average cost: {ex.Message}" }, null));
        }
    }

    public async Task<Result<TimeSpan>> GetAverageExecutionDurationAsync()
    {
        try
        {
            var completedExecutions = await _dbSet
                .Where(pe => pe.Status == "Completed" && pe.CompletedAt.HasValue)
                .Select(pe => new { pe.StartedAt, pe.CompletedAt })
                .ToListAsync();

            if (!completedExecutions.Any()) return Result.Success(TimeSpan.Zero);

            var durations = completedExecutions
                .Where(e => e.CompletedAt.HasValue)
                .Select(e => e.CompletedAt!.Value - e.StartedAt)
                .ToList();

            if (!durations.Any()) return Result.Success(TimeSpan.Zero);

            var averageTicks = durations.Select(d => d.Ticks).Average();
            return Result.Success(new TimeSpan((long)averageTicks));
        }
        catch (Exception ex)
        {
            return Result.Error(new ErrorList(new[] { $"Error calculating average execution duration: {ex.Message}" }, null));
        }
    }

    public async Task<Result<(decimal TotalCost, int TotalTokens, TimeSpan TotalDuration)>> GetAggregateMetricsAsync(Guid projectId)
    {
        try
        {
            var executions = await _dbSet
                .Where(pe => pe.ProjectId == projectId)
                .Include(pe => pe.AgentExecutions)
                .ToListAsync();

            // Calculate from AgentExecutions since PipelineExecution doesn't have TotalCost/TotalTokens
            var totalCost = executions
                .SelectMany(pe => pe.AgentExecutions)
                .Where(ae => ae.ExecutionCost.HasValue)
                .Sum(ae => ae.ExecutionCost!.Value);
                
            var totalTokens = executions
                .SelectMany(pe => pe.AgentExecutions)
                .Where(ae => ae.InputTokens.HasValue && ae.OutputTokens.HasValue)
                .Sum(ae => ae.InputTokens!.Value + ae.OutputTokens!.Value);
            
            var completedExecutions = executions.Where(pe => pe.CompletedAt.HasValue);
            var totalDuration = TimeSpan.FromTicks(
                completedExecutions.Sum(pe => (pe.CompletedAt!.Value - pe.StartedAt).Ticks)
            );

            return Result.Success((totalCost, totalTokens, totalDuration));
        }
        catch (Exception ex)
        {
            return Result.Error(new ErrorList(new[] { $"Error retrieving aggregate metrics for project {projectId}: {ex.Message}" }, null));
        }
    }

    // Business operations
    public async Task<Result<PipelineExecution>> StartExecutionAsync(Guid projectId, string orchestrationStrategy)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(orchestrationStrategy))
                return Result.Invalid(new ValidationError { ErrorMessage = "Orchestration strategy cannot be null or empty" });

            var execution = new PipelineExecution
            {
                ProjectId = projectId,
                Status = "Running",
                StartedAt = DateTime.UtcNow,
                ProgressPercentage = 0
            };

            // Note: OrchestrationStrategy isn't a direct property of PipelineExecution
            // This could be stored in ExecutionConfig or ExecutionMetrics as JSON if needed

            var addResult = await AddAsync(execution);
            if (!addResult.IsSuccess)
                return Result.Error(new ErrorList(new[] { $"Failed to add pipeline execution: {string.Join(", ", addResult.Errors)}" }, null));

            var saveResult = await SaveChangesAsync();
            if (!saveResult.IsSuccess)
                return Result.Error(new ErrorList(new[] { $"Failed to save pipeline execution: {string.Join(", ", saveResult.Errors)}" }, null));

            return Result.Success(execution);
        }
        catch (Exception ex)
        {
            return Result.Error(new ErrorList(new[] { $"Error starting pipeline execution for project {projectId}: {ex.Message}" }, null));
        }
    }

    public async Task<Result> UpdateExecutionStatusAsync(Guid id, string status)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(status))
                return Result.Invalid(new ValidationError { ErrorMessage = "Status cannot be null or empty" });

            var executionResult = await GetByIdAsync(id);
            if (!executionResult.IsSuccess)
                return Result.NotFound($"Pipeline execution with ID {id} not found");

            var execution = executionResult.Value;
            execution.Status = status;
            
            if (status == "Completed" || status == "Failed")
                execution.CompletedAt = DateTime.UtcNow;

            var updateResult = await UpdateAsync(execution);
            if (!updateResult.IsSuccess)
                return Result.Error(new ErrorList(new[] { $"Failed to update pipeline execution: {string.Join(", ", updateResult.Errors)}" }, null));

            var saveResult = await SaveChangesAsync();
            if (!saveResult.IsSuccess)
                return Result.Error(new ErrorList(new[] { $"Failed to save pipeline execution status update: {string.Join(", ", saveResult.Errors)}" }, null));

            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Error(new ErrorList(new[] { $"Error updating execution status for ID {id}: {ex.Message}" }, null));
        }
    }

    public async Task<Result> UpdateProgressAsync(Guid id, int progressPercentage)
    {
        try
        {
            if (progressPercentage < 0 || progressPercentage > 100)
                return Result.Invalid(new ValidationError { ErrorMessage = "Progress percentage must be between 0 and 100" });

            var executionResult = await GetByIdAsync(id);
            if (!executionResult.IsSuccess)
                return Result.NotFound($"Pipeline execution with ID {id} not found");

            var execution = executionResult.Value;
            execution.ProgressPercentage = progressPercentage;
            
            var updateResult = await UpdateAsync(execution);
            if (!updateResult.IsSuccess)
                return Result.Error(new ErrorList(new[] { $"Failed to update pipeline execution: {string.Join(", ", updateResult.Errors)}" }, null));

            var saveResult = await SaveChangesAsync();
            if (!saveResult.IsSuccess)
                return Result.Error(new ErrorList(new[] { $"Failed to save pipeline execution progress update: {string.Join(", ", saveResult.Errors)}" }, null));

            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Error(new ErrorList(new[] { $"Error updating progress for pipeline execution ID {id}: {ex.Message}" }, null));
        }
    }

    public async Task<Result> CompleteExecutionAsync(Guid id, bool success = true)
    {
        try
        {
            var executionResult = await GetByIdAsync(id);
            if (!executionResult.IsSuccess)
                return Result.NotFound($"Pipeline execution with ID {id} not found");

            var execution = executionResult.Value;
            execution.Status = success ? "Completed" : "Failed";
            execution.CompletedAt = DateTime.UtcNow;
            execution.ProgressPercentage = success ? 100 : execution.ProgressPercentage;

            var updateResult = await UpdateAsync(execution);
            if (!updateResult.IsSuccess)
                return Result.Error(new ErrorList(new[] { $"Failed to update pipeline execution: {string.Join(", ", updateResult.Errors)}" }, null));

            var saveResult = await SaveChangesAsync();
            if (!saveResult.IsSuccess)
                return Result.Error(new ErrorList(new[] { $"Failed to save pipeline execution completion: {string.Join(", ", saveResult.Errors)}" }, null));

            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Error(new ErrorList(new[] { $"Error completing pipeline execution ID {id}: {ex.Message}" }, null));
        }
    }

    public async Task<Result> FailExecutionAsync(Guid id, string? errorMessage = null)
    {
        try
        {
            var executionResult = await GetByIdAsync(id);
            if (!executionResult.IsSuccess)
                return Result.NotFound($"Pipeline execution with ID {id} not found");

            var execution = executionResult.Value;
            execution.Status = "Failed";
            execution.CompletedAt = DateTime.UtcNow;
            if (!string.IsNullOrEmpty(errorMessage))
                execution.ErrorMessage = errorMessage;

            var updateResult = await UpdateAsync(execution);
            if (!updateResult.IsSuccess)
                return Result.Error(new ErrorList(new[] { $"Failed to update pipeline execution: {string.Join(", ", updateResult.Errors)}" }, null));

            var saveResult = await SaveChangesAsync();
            if (!saveResult.IsSuccess)
                return Result.Error(new ErrorList(new[] { $"Failed to save pipeline execution failure: {string.Join(", ", saveResult.Errors)}" }, null));

            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Error(new ErrorList(new[] { $"Error failing pipeline execution ID {id}: {ex.Message}" }, null));
        }
    }

    // Stage operations
    public async Task<Result<IEnumerable<StageExecution>>> GetStagesForExecutionAsync(Guid executionId)
    {
        try
        {
            var stages = await _context.StageExecutions
                .Where(se => se.PipelineExecutionId == executionId)
                .OrderBy(se => se.ExecutionOrder)
                .Include(se => se.AgentExecutions)
                .ToListAsync();
            
            return Result.Success(stages.AsEnumerable());
        }
        catch (Exception ex)
        {
            return Result.Error(new ErrorList(new[] { $"Error retrieving stages for pipeline execution {executionId}: {ex.Message}" }, null));
        }
    }

    public async Task<Result<StageExecution>> GetCurrentStageAsync(Guid executionId)
    {
        try
        {
            var stage = await _context.StageExecutions
                .Where(se => se.PipelineExecutionId == executionId && 
                            (se.Status == "Running" || se.Status == "InProgress"))
                .OrderBy(se => se.ExecutionOrder)
                .FirstOrDefaultAsync();
            
            return stage != null 
                ? Result.Success(stage)
                : Result.NotFound($"No current stage found for pipeline execution {executionId}");
        }
        catch (Exception ex)
        {
            return Result.Error(new ErrorList(new[] { $"Error retrieving current stage for pipeline execution {executionId}: {ex.Message}" }, null));
        }
    }

    public async Task<Result<StageExecution>> GetStageByNameAsync(Guid executionId, string stageName)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(stageName))
                return Result.Invalid(new ValidationError { ErrorMessage = "Stage name cannot be null or empty" });

            var stage = await _context.StageExecutions
                .Where(se => se.PipelineExecutionId == executionId && se.StageName == stageName)
                .Include(se => se.AgentExecutions)
                .FirstOrDefaultAsync();
            
            return stage != null 
                ? Result.Success(stage)
                : Result.NotFound($"Stage '{stageName}' not found for pipeline execution {executionId}");
        }
        catch (Exception ex)
        {
            return Result.Error(new ErrorList(new[] { $"Error retrieving stage '{stageName}' for pipeline execution {executionId}: {ex.Message}" }, null));
        }
    }

    // Time-based queries
    public async Task<Result<IEnumerable<PipelineExecution>>> GetExecutionsStartedBetweenAsync(DateTime startDate, DateTime endDate)
    {
        try
        {
            if (startDate > endDate)
                return Result.Invalid(new ValidationError { ErrorMessage = "Start date cannot be later than end date" });

            var executions = await _dbSet
                .Where(pe => pe.StartedAt >= startDate && pe.StartedAt <= endDate)
                .OrderByDescending(pe => pe.StartedAt)
                .ToListAsync();
            
            return Result.Success(executions.AsEnumerable());
        }
        catch (Exception ex)
        {
            return Result.Error(new ErrorList(new[] { $"Error retrieving executions between {startDate:yyyy-MM-dd} and {endDate:yyyy-MM-dd}: {ex.Message}" }, null));
        }
    }

    public async Task<Result<IEnumerable<PipelineExecution>>> GetLongRunningExecutionsAsync(TimeSpan threshold)
    {
        try
        {
            if (threshold <= TimeSpan.Zero)
                return Result.Invalid(new ValidationError { ErrorMessage = "Threshold must be greater than zero" });

            var cutoffTime = DateTime.UtcNow.Subtract(threshold);
            var executions = await _dbSet
                .Where(pe => pe.StartedAt <= cutoffTime && 
                            (pe.Status == "Running" || pe.Status == "InProgress"))
                .OrderBy(pe => pe.StartedAt)
                .ToListAsync();
            
            return Result.Success(executions.AsEnumerable());
        }
        catch (Exception ex)
        {
            return Result.Error(new ErrorList(new[] { $"Error retrieving long running executions with threshold {threshold}: {ex.Message}" }, null));
        }
    }

    public async Task<Result<IEnumerable<PipelineExecution>>> GetRecentExecutionsAsync(int count = 20)
    {
        try
        {
            if (count <= 0)
                return Result.Invalid(new ValidationError { ErrorMessage = "Count must be greater than zero" });

            var executions = await _dbSet
                .OrderByDescending(pe => pe.StartedAt)
                .Take(count)
                .ToListAsync();
            
            return Result.Success(executions.AsEnumerable());
        }
        catch (Exception ex)
        {
            return Result.Error(new ErrorList(new[] { $"Error retrieving recent {count} executions: {ex.Message}" }, null));
        }
    }

    // Orchestration pattern queries
    public async Task<Result<IEnumerable<PipelineExecution>>> GetByOrchestrationStrategyAsync(string strategy)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(strategy))
                return Result.Invalid(new ValidationError { ErrorMessage = "Strategy cannot be null or empty" });

            // OrchestrationStrategy isn't a direct property, could check ExecutionConfig JSON field if implemented
            // For now, return empty list for interface compatibility
            return Result.Success((IEnumerable<PipelineExecution>)new List<PipelineExecution>());
        }
        catch (Exception ex)
        {
            return Result.Error(new ErrorList(new[] { $"Error retrieving executions by orchestration strategy '{strategy}': {ex.Message}" }, null));
        }
    }

    public async Task<Result<IEnumerable<(string Strategy, int Count, decimal AvgDuration)>>> GetOrchestrationEfficiencyAsync()
    {
        try
        {
            // OrchestrationStrategy isn't a direct property of PipelineExecution
            // Could implement by parsing ExecutionConfig JSON if orchestration strategy is stored there
            // For now, return empty list for interface compatibility
            return Result.Success((IEnumerable<(string, int, decimal)>)new List<(string, int, decimal)>());
        }
        catch (Exception ex)
        {
            return Result.Error(new ErrorList(new[] { $"Error retrieving orchestration efficiency metrics: {ex.Message}" }, null));
        }
    }
}