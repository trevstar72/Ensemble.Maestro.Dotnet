using Ensemble.Maestro.Dotnet.Core.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Ardalis.Result;

namespace Ensemble.Maestro.Dotnet.Core.Data.Repositories;

/// <summary>
/// Repository implementation for AgentExecution entities with specialized operations
/// </summary>
public class AgentExecutionRepository : Repository<AgentExecution>, IAgentExecutionRepository
{
    public AgentExecutionRepository(MaestroDbContext context) : base(context)
    {
    }

    // Query operations
    public async Task<Result<IEnumerable<AgentExecution>>> GetByProjectIdAsync(Guid projectId)
    {
        try
        {
            var executions = await _dbSet
                .Where(ae => ae.ProjectId == projectId)
                .OrderByDescending(ae => ae.StartedAt)
                .ToListAsync();
            return Result.Success(executions.AsEnumerable());
        }
        catch (Exception ex)
        {
            return Result.Error(new ErrorList(new[] { $"Error retrieving agent executions for project {projectId}: {ex.Message}" }, null));
        }
    }

    public async Task<Result<IEnumerable<AgentExecution>>> GetByPipelineExecutionIdAsync(Guid pipelineExecutionId)
    {
        try
        {
            var executions = await _dbSet
                .Where(ae => ae.PipelineExecutionId == pipelineExecutionId)
                .OrderBy(ae => ae.StartedAt)
                .ToListAsync();
            return Result.Success(executions.AsEnumerable());
        }
        catch (Exception ex)
        {
            return Result.Error(new ErrorList(new[] { $"Error retrieving agent executions for pipeline {pipelineExecutionId}: {ex.Message}" }, null));
        }
    }

    public async Task<Result<IEnumerable<AgentExecution>>> GetByStageExecutionIdAsync(Guid stageExecutionId)
    {
        try
        {
            var executions = await _dbSet
                .Where(ae => ae.StageExecutionId == stageExecutionId)
                .OrderBy(ae => ae.StartedAt)
                .ToListAsync();
            return Result.Success(executions.AsEnumerable());
        }
        catch (Exception ex)
        {
            return Result.Error(new ErrorList(new[] { $"Error retrieving agent executions for stage {stageExecutionId}: {ex.Message}" }, null));
        }
    }

    public async Task<Result<IEnumerable<AgentExecution>>> GetByAgentTypeAsync(string agentType)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(agentType))
                return Result.Invalid(new ValidationError { ErrorMessage = "Agent type cannot be null or empty" });

            var executions = await _dbSet
                .Where(ae => ae.AgentType == agentType)
                .OrderByDescending(ae => ae.StartedAt)
                .ToListAsync();
            return Result.Success(executions.AsEnumerable());
        }
        catch (Exception ex)
        {
            return Result.Error(new ErrorList(new[] { $"Error retrieving agent executions by type {agentType}: {ex.Message}" }, null));
        }
    }

    public async Task<Result<IEnumerable<AgentExecution>>> GetByStatusAsync(string status)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(status))
                return Result.Invalid(new ValidationError { ErrorMessage = "Status cannot be null or empty" });

            var executions = await _dbSet
                .Where(ae => ae.Status == status)
                .OrderByDescending(ae => ae.StartedAt)
                .ToListAsync();
            return Result.Success(executions.AsEnumerable());
        }
        catch (Exception ex)
        {
            return Result.Error(new ErrorList(new[] { $"Error retrieving agent executions by status {status}: {ex.Message}" }, null));
        }
    }

    public async Task<Result<IEnumerable<AgentExecution>>> GetActiveExecutionsAsync()
    {
        try
        {
            var executions = await _dbSet
                .Where(ae => ae.Status == "Running" || ae.Status == "InProgress" || ae.Status == "Queued")
                .OrderBy(ae => ae.StartedAt)
                .ToListAsync();
            return Result.Success(executions.AsEnumerable());
        }
        catch (Exception ex)
        {
            return Result.Error(new ErrorList(new[] { $"Error retrieving active executions: {ex.Message}" }, null));
        }
    }

    public async Task<Result<IEnumerable<AgentExecution>>> GetCompletedExecutionsAsync()
    {
        try
        {
            var executions = await _dbSet
                .Where(ae => ae.Status == "Completed")
                .OrderByDescending(ae => ae.CompletedAt)
                .ToListAsync();
            return Result.Success(executions.AsEnumerable());
        }
        catch (Exception ex)
        {
            return Result.Error(new ErrorList(new[] { $"Error retrieving completed executions: {ex.Message}" }, null));
        }
    }

    public async Task<Result<IEnumerable<AgentExecution>>> GetFailedExecutionsAsync()
    {
        try
        {
            var executions = await _dbSet
                .Where(ae => ae.Status == "Failed" || ae.Status == "Error")
                .OrderByDescending(ae => ae.StartedAt)
                .ToListAsync();
            return Result.Success(executions.AsEnumerable());
        }
        catch (Exception ex)
        {
            return Result.Error(new ErrorList(new[] { $"Error retrieving failed executions: {ex.Message}" }, null));
        }
    }

    // Hierarchical operations
    public async Task<Result<IEnumerable<AgentExecution>>> GetChildExecutionsAsync(Guid parentId)
    {
        try
        {
            var executions = await _dbSet
                .Where(ae => ae.ParentExecutionId == parentId)
                .OrderBy(ae => ae.StartedAt)
                .ToListAsync();
            return Result.Success(executions.AsEnumerable());
        }
        catch (Exception ex)
        {
            return Result.Error(new ErrorList(new[] { $"Error retrieving child executions for parent {parentId}: {ex.Message}" }, null));
        }
    }

    public async Task<Result<AgentExecution>> GetParentExecutionAsync(Guid childId)
    {
        try
        {
            var child = await _dbSet.FirstOrDefaultAsync(ae => ae.Id == childId);
            if (child?.ParentExecutionId == null) 
                return Result.NotFound("Child execution not found or has no parent");

            var parentResult = await GetByIdAsync(child.ParentExecutionId.Value);
            return parentResult.IsSuccess 
                ? Result.Success(parentResult.Value) 
                : Result.NotFound("Parent execution not found");
        }
        catch (Exception ex)
        {
            return Result.Error(new ErrorList(new[] { $"Error retrieving parent execution for child {childId}: {ex.Message}" }, null));
        }
    }

    public async Task<Result<IEnumerable<AgentExecution>>> GetExecutionHierarchyAsync(Guid rootId)
    {
        try
        {
            var result = new List<AgentExecution>();
            await GetHierarchyRecursive(rootId, result);
            return Result.Success(result.AsEnumerable());
        }
        catch (Exception ex)
        {
            return Result.Error(new ErrorList(new[] { $"Error retrieving execution hierarchy for root {rootId}: {ex.Message}" }, null));
        }
    }

    private async Task GetHierarchyRecursive(Guid executionId, List<AgentExecution> result)
    {
        var executionResult = await GetByIdAsync(executionId);
        if (!executionResult.IsSuccess) return;
        
        result.Add(executionResult.Value);
        var childrenResult = await GetChildExecutionsAsync(executionId);
        
        if (childrenResult.IsSuccess)
        {
            foreach (var child in childrenResult.Value)
            {
                await GetHierarchyRecursive(child.Id, result);
            }
        }
    }

    public async Task<Result<int>> GetHierarchyDepthAsync(Guid executionId)
    {
        try
        {
            var depth = 0;
            var currentId = executionId;

            while (depth <= 100) // Prevent infinite loops
            {
                var execution = await _dbSet.FirstOrDefaultAsync(ae => ae.Id == currentId);
                if (execution?.ParentExecutionId == null) break;
                
                depth++;
                currentId = execution.ParentExecutionId.Value;
            }

            return Result.Success(depth);
        }
        catch (Exception ex)
        {
            return Result.Error(new ErrorList(new[] { $"Error calculating hierarchy depth for execution {executionId}: {ex.Message}" }, null));
        }
    }

    // Complex queries with related data
    public async Task<Result<AgentExecution>> GetWithMessagesAsync(Guid id)
    {
        try
        {
            var execution = await _dbSet
                .Include(ae => ae.Messages.OrderBy(m => m.CreatedAt))
                .FirstOrDefaultAsync(ae => ae.Id == id);
            
            return execution != null 
                ? Result.Success(execution) 
                : Result.NotFound($"Agent execution {id} not found");
        }
        catch (Exception ex)
        {
            return Result.Error(new ErrorList(new[] { $"Error retrieving execution {id} with messages: {ex.Message}" }, null));
        }
    }

    public async Task<Result<AgentExecution>> GetWithAllRelatedDataAsync(Guid id)
    {
        try
        {
            var execution = await _dbSet
                .Include(ae => ae.Messages.OrderBy(m => m.CreatedAt))
                .Include(ae => ae.Project)
                .Include(ae => ae.PipelineExecution)
                .Include(ae => ae.StageExecution)
                .Include(ae => ae.ParentExecution)
                .Include(ae => ae.ChildExecutions)
                .FirstOrDefaultAsync(ae => ae.Id == id);
            
            return execution != null 
                ? Result.Success(execution) 
                : Result.NotFound($"Agent execution {id} not found");
        }
        catch (Exception ex)
        {
            return Result.Error(new ErrorList(new[] { $"Error retrieving execution {id} with all related data: {ex.Message}" }, null));
        }
    }

    public async Task<Result<IEnumerable<AgentExecution>>> GetExecutionsWithMessagesAsync(Guid projectId)
    {
        try
        {
            var executions = await _dbSet
                .Where(ae => ae.ProjectId == projectId)
                .Include(ae => ae.Messages.OrderBy(m => m.CreatedAt))
                .OrderByDescending(ae => ae.StartedAt)
                .ToListAsync();
            return Result.Success(executions.AsEnumerable());
        }
        catch (Exception ex)
        {
            return Result.Error(new ErrorList(new[] { $"Error retrieving executions with messages for project {projectId}: {ex.Message}" }, null));
        }
    }

    // Analytics and statistics
    public async Task<Result<int>> GetExecutionCountByAgentTypeAsync(string agentType)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(agentType))
                return Result.Invalid(new ValidationError { ErrorMessage = "Agent type cannot be null or empty" });

            var count = await _dbSet.CountAsync(ae => ae.AgentType == agentType);
            return Result.Success(count);
        }
        catch (Exception ex)
        {
            return Result.Error(new ErrorList(new[] { $"Error counting executions by agent type {agentType}: {ex.Message}" }, null));
        }
    }

    public async Task<Result<int>> GetExecutionCountByStatusAsync(string status)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(status))
                return Result.Invalid(new ValidationError { ErrorMessage = "Status cannot be null or empty" });

            var count = await _dbSet.CountAsync(ae => ae.Status == status);
            return Result.Success(count);
        }
        catch (Exception ex)
        {
            return Result.Error(new ErrorList(new[] { $"Error counting executions by status {status}: {ex.Message}" }, null));
        }
    }

    public async Task<Result<(int Total, int Running, int Completed, int Failed)>> GetExecutionStatisticsAsync()
    {
        try
        {
            var executions = await _dbSet.ToListAsync();
            var total = executions.Count;
            var running = executions.Count(ae => ae.Status == "Running" || ae.Status == "InProgress" || ae.Status == "Queued");
            var completed = executions.Count(ae => ae.Status == "Completed");
            var failed = executions.Count(ae => ae.Status == "Failed" || ae.Status == "Error");

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
            var executions = await _dbSet.Where(ae => ae.ProjectId == projectId).ToListAsync();
            var total = executions.Count;
            var running = executions.Count(ae => ae.Status == "Running" || ae.Status == "InProgress" || ae.Status == "Queued");
            var completed = executions.Count(ae => ae.Status == "Completed");
            var failed = executions.Count(ae => ae.Status == "Failed" || ae.Status == "Error");

            return Result.Success((total, running, completed, failed));
        }
        catch (Exception ex)
        {
            return Result.Error(new ErrorList(new[] { $"Error retrieving execution statistics for project {projectId}: {ex.Message}" }, null));
        }
    }

    public async Task<Result<(int Total, int Running, int Completed, int Failed)>> GetExecutionStatisticsForPipelineAsync(Guid pipelineExecutionId)
    {
        try
        {
            var executions = await _dbSet.Where(ae => ae.PipelineExecutionId == pipelineExecutionId).ToListAsync();
            var total = executions.Count;
            var running = executions.Count(ae => ae.Status == "Running" || ae.Status == "InProgress" || ae.Status == "Queued");
            var completed = executions.Count(ae => ae.Status == "Completed");
            var failed = executions.Count(ae => ae.Status == "Failed" || ae.Status == "Error");

            return Result.Success((total, running, completed, failed));
        }
        catch (Exception ex)
        {
            return Result.Error(new ErrorList(new[] { $"Error retrieving execution statistics for pipeline {pipelineExecutionId}: {ex.Message}" }, null));
        }
    }

    public async Task<Result<IEnumerable<(string AgentType, int Count)>>> GetAgentTypeDistributionAsync()
    {
        try
        {
            var distribution = await _dbSet
                .GroupBy(ae => ae.AgentType)
                .Select(g => new { AgentType = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Select(x => ValueTuple.Create(x.AgentType, x.Count))
                .ToListAsync();
            return Result.Success(distribution.AsEnumerable());
        }
        catch (Exception ex)
        {
            return Result.Error(new ErrorList(new[] { $"Error retrieving agent type distribution: {ex.Message}" }, null));
        }
    }

    public async Task<Result<IEnumerable<(string Status, int Count)>>> GetStatusDistributionAsync()
    {
        try
        {
            var distribution = await _dbSet
                .GroupBy(ae => ae.Status)
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

    // Performance metrics
    public async Task<Result<decimal>> GetAverageExecutionTimeByAgentTypeAsync(string agentType)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(agentType))
                return Result.Invalid(new ValidationError { ErrorMessage = "Agent type cannot be null or empty" });

            var executions = await _dbSet
                .Where(ae => ae.AgentType == agentType && ae.Status == "Completed" && ae.CompletedAt.HasValue)
                .Select(ae => new { ae.StartedAt, ae.CompletedAt })
                .ToListAsync();

            if (!executions.Any()) return Result.Success(0m);

            var durations = executions
                .Where(e => e.CompletedAt.HasValue)
                .Select(e => (decimal)(e.CompletedAt!.Value - e.StartedAt).TotalSeconds)
                .ToList();

            var average = durations.Any() ? durations.Average() : 0m;
            return Result.Success(average);
        }
        catch (Exception ex)
        {
            return Result.Error(new ErrorList(new[] { $"Error calculating average execution time for agent type {agentType}: {ex.Message}" }, null));
        }
    }

    public async Task<Result<decimal>> GetAverageTokenUsageByAgentTypeAsync(string agentType)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(agentType))
                return Result.Invalid(new ValidationError { ErrorMessage = "Agent type cannot be null or empty" });

            var tokenCounts = await _dbSet
                .Where(ae => ae.AgentType == agentType && ae.InputTokens.HasValue && ae.OutputTokens.HasValue)
                .Select(ae => ae.InputTokens!.Value + ae.OutputTokens!.Value)
                .ToListAsync();

            var average = tokenCounts.Any() ? (decimal)tokenCounts.Average() : 0m;
            return Result.Success(average);
        }
        catch (Exception ex)
        {
            return Result.Error(new ErrorList(new[] { $"Error calculating average token usage for agent type {agentType}: {ex.Message}" }, null));
        }
    }

    public async Task<Result<decimal>> GetAverageCostByAgentTypeAsync(string agentType)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(agentType))
                return Result.Invalid(new ValidationError { ErrorMessage = "Agent type cannot be null or empty" });

            var costs = await _dbSet
                .Where(ae => ae.AgentType == agentType && ae.ExecutionCost.HasValue)
                .Select(ae => ae.ExecutionCost!.Value)
                .ToListAsync();

            var average = costs.Any() ? costs.Average() : 0m;
            return Result.Success(average);
        }
        catch (Exception ex)
        {
            return Result.Error(new ErrorList(new[] { $"Error calculating average cost for agent type {agentType}: {ex.Message}" }, null));
        }
    }

    public async Task<Result<(decimal TotalCost, int TotalInputTokens, int TotalOutputTokens)>> GetAggregateMetricsAsync(Guid projectId)
    {
        try
        {
            var executions = await _dbSet
                .Where(ae => ae.ProjectId == projectId)
                .ToListAsync();

            var totalCost = executions.Where(ae => ae.ExecutionCost.HasValue).Sum(ae => ae.ExecutionCost!.Value);
            var totalInputTokens = executions.Where(ae => ae.InputTokens.HasValue).Sum(ae => ae.InputTokens!.Value);
            var totalOutputTokens = executions.Where(ae => ae.OutputTokens.HasValue).Sum(ae => ae.OutputTokens!.Value);

            return Result.Success((totalCost, totalInputTokens, totalOutputTokens));
        }
        catch (Exception ex)
        {
            return Result.Error(new ErrorList(new[] { $"Error calculating aggregate metrics for project {projectId}: {ex.Message}" }, null));
        }
    }

    public async Task<Result<(decimal TotalCost, int TotalInputTokens, int TotalOutputTokens)>> GetAggregateMetricsForPipelineAsync(Guid pipelineExecutionId)
    {
        try
        {
            var executions = await _dbSet
                .Where(ae => ae.PipelineExecutionId == pipelineExecutionId)
                .ToListAsync();

            var totalCost = executions.Where(ae => ae.ExecutionCost.HasValue).Sum(ae => ae.ExecutionCost!.Value);
            var totalInputTokens = executions.Where(ae => ae.InputTokens.HasValue).Sum(ae => ae.InputTokens!.Value);
            var totalOutputTokens = executions.Where(ae => ae.OutputTokens.HasValue).Sum(ae => ae.OutputTokens!.Value);

            return Result.Success((totalCost, totalInputTokens, totalOutputTokens));
        }
        catch (Exception ex)
        {
            return Result.Error(new ErrorList(new[] { $"Error calculating aggregate metrics for pipeline {pipelineExecutionId}: {ex.Message}" }, null));
        }
    }

    public async Task<Result<IEnumerable<(string AgentType, decimal AvgCost, int AvgTokens, decimal AvgDuration)>>> GetAgentEfficiencyMetricsAsync()
    {
        try
        {
            var executions = await _dbSet
                .Where(ae => ae.Status == "Completed" && ae.CompletedAt.HasValue)
                .ToListAsync();

            var metrics = executions
                .GroupBy(ae => ae.AgentType)
                .Select(g => new
                {
                    AgentType = g.Key,
                    AvgCost = g.Where(ae => ae.ExecutionCost.HasValue).Any() 
                        ? g.Where(ae => ae.ExecutionCost.HasValue).Average(ae => ae.ExecutionCost!.Value) 
                        : 0m,
                    AvgTokens = g.Where(ae => ae.InputTokens.HasValue && ae.OutputTokens.HasValue).Any()
                        ? (int)g.Where(ae => ae.InputTokens.HasValue && ae.OutputTokens.HasValue)
                               .Average(ae => ae.InputTokens!.Value + ae.OutputTokens!.Value)
                        : 0,
                    AvgDuration = (decimal)g.Average(ae => (ae.CompletedAt!.Value - ae.StartedAt).TotalSeconds)
                })
                .Select(x => ValueTuple.Create(x.AgentType, x.AvgCost, x.AvgTokens, x.AvgDuration))
                .ToList();

            return Result.Success(metrics.AsEnumerable());
        }
        catch (Exception ex)
        {
            return Result.Error(new ErrorList(new[] { $"Error calculating agent efficiency metrics: {ex.Message}" }, null));
        }
    }

    // Message operations
    public async Task<Result<IEnumerable<AgentMessage>>> GetMessagesForExecutionAsync(Guid executionId)
    {
        try
        {
            var messages = await _context.AgentMessages
                .Where(m => m.AgentExecutionId == executionId)
                .OrderBy(m => m.CreatedAt)
                .ToListAsync();
            return Result.Success(messages.AsEnumerable());
        }
        catch (Exception ex)
        {
            return Result.Error(new ErrorList(new[] { $"Error retrieving messages for execution {executionId}: {ex.Message}" }, null));
        }
    }

    public async Task<Result<int>> GetMessageCountForExecutionAsync(Guid executionId)
    {
        try
        {
            var count = await _context.AgentMessages
                .CountAsync(m => m.AgentExecutionId == executionId);
            return Result.Success(count);
        }
        catch (Exception ex)
        {
            return Result.Error(new ErrorList(new[] { $"Error counting messages for execution {executionId}: {ex.Message}" }, null));
        }
    }

    public async Task<Result<AgentMessage>> GetLatestMessageAsync(Guid executionId)
    {
        try
        {
            var message = await _context.AgentMessages
                .Where(m => m.AgentExecutionId == executionId)
                .OrderByDescending(m => m.CreatedAt)
                .FirstOrDefaultAsync();
            
            return message != null 
                ? Result.Success(message) 
                : Result.NotFound($"No messages found for execution {executionId}");
        }
        catch (Exception ex)
        {
            return Result.Error(new ErrorList(new[] { $"Error retrieving latest message for execution {executionId}: {ex.Message}" }, null));
        }
    }

    public async Task<Result<IEnumerable<AgentMessage>>> GetConversationHistoryAsync(Guid executionId)
    {
        try
        {
            var messages = await _context.AgentMessages
                .Where(m => m.AgentExecutionId == executionId)
                .OrderBy(m => m.CreatedAt)
                .ToListAsync();
            return Result.Success(messages.AsEnumerable());
        }
        catch (Exception ex)
        {
            return Result.Error(new ErrorList(new[] { $"Error retrieving conversation history for execution {executionId}: {ex.Message}" }, null));
        }
    }

    // Business operations
    public async Task<Result<AgentExecution>> StartExecutionAsync(Guid projectId, Guid pipelineExecutionId, string agentType, string agentName)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(agentType))
                return Result.Invalid(new ValidationError { ErrorMessage = "Agent type cannot be null or empty" });
            if (string.IsNullOrWhiteSpace(agentName))
                return Result.Invalid(new ValidationError { ErrorMessage = "Agent name cannot be null or empty" });

            var execution = new AgentExecution
            {
                ProjectId = projectId,
                PipelineExecutionId = pipelineExecutionId,
                AgentType = agentType,
                AgentName = agentName,
                Status = "Running",
                StartedAt = DateTime.UtcNow
            };

            var addResult = await AddAsync(execution);
            if (!addResult.IsSuccess) 
                return Result.Error($"Failed to add execution: {addResult.Errors.FirstOrDefault()}");
            
            var saveResult = await SaveChangesAsync();
            return saveResult.IsSuccess 
                ? Result.Success(execution)
                : Result.Error(new ErrorList(new[] { $"Failed to save execution: {saveResult.Errors.FirstOrDefault()}" }, null));
        }
        catch (Exception ex)
        {
            return Result.Error(new ErrorList(new[] { $"Error starting execution: {ex.Message}" }, null));
        }
    }

    public async Task<Result<AgentExecution>> StartChildExecutionAsync(Guid parentId, string agentType, string agentName)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(agentType))
                return Result.Invalid(new ValidationError { ErrorMessage = "Agent type cannot be null or empty" });
            if (string.IsNullOrWhiteSpace(agentName))
                return Result.Invalid(new ValidationError { ErrorMessage = "Agent name cannot be null or empty" });

            var parentResult = await GetByIdAsync(parentId);
            if (!parentResult.IsSuccess) return Result.Error(new ErrorList(new[] { "Parent execution not found" }, null));

            var parent = parentResult.Value;
            var execution = new AgentExecution
            {
                ProjectId = parent.ProjectId,
                PipelineExecutionId = parent.PipelineExecutionId,
                StageExecutionId = parent.StageExecutionId,
                ParentExecutionId = parentId,
                AgentType = agentType,
                AgentName = agentName,
                Status = "Running",
                StartedAt = DateTime.UtcNow
            };

            var addResult = await AddAsync(execution);
            if (!addResult.IsSuccess) 
                return Result.Error($"Failed to add child execution: {addResult.Errors.FirstOrDefault()}");
            
            var saveResult = await SaveChangesAsync();
            return saveResult.IsSuccess 
                ? Result.Success(execution)
                : Result.Error(new ErrorList(new[] { $"Failed to save child execution: {saveResult.Errors.FirstOrDefault()}" }, null));
        }
        catch (Exception ex)
        {
            return Result.Error(new ErrorList(new[] { $"Error starting child execution: {ex.Message}" }, null));
        }
    }

    public async Task<Result> UpdateExecutionStatusAsync(Guid id, string status)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(status))
                return Result.Invalid(new ValidationError { ErrorMessage = "Status cannot be null or empty" });

            var executionResult = await GetByIdAsync(id);
            if (!executionResult.IsSuccess) return Result.NotFound($"Execution {id} not found");

            var execution = executionResult.Value;
            execution.Status = status;
            
            if (status == "Completed" || status == "Failed")
                execution.CompletedAt = DateTime.UtcNow;

            var updateResult = await UpdateAsync(execution);
            if (!updateResult.IsSuccess) 
                return Result.Error(new ErrorList(new[] { $"Failed to update execution: {updateResult.Errors.FirstOrDefault()}" }, null));
            
            var saveResult = await SaveChangesAsync();
            return saveResult.IsSuccess 
                ? Result.Success() 
                : Result.Error(new ErrorList(new[] { $"Failed to save execution status: {saveResult.Errors.FirstOrDefault()}" }, null));
        }
        catch (Exception ex)
        {
            return Result.Error(new ErrorList(new[] { $"Error updating execution status: {ex.Message}" }, null));
        }
    }

    public async Task<Result> CompleteExecutionAsync(Guid id, bool success = true)
    {
        try
        {
            var executionResult = await GetByIdAsync(id);
            if (!executionResult.IsSuccess) return Result.NotFound($"Execution {id} not found");

            var execution = executionResult.Value;
            execution.Status = success ? "Completed" : "Failed";
            execution.CompletedAt = DateTime.UtcNow;

            var updateResult = await UpdateAsync(execution);
            if (!updateResult.IsSuccess) 
                return Result.Error(new ErrorList(new[] { $"Failed to update execution: {updateResult.Errors.FirstOrDefault()}" }, null));
            
            var saveResult = await SaveChangesAsync();
            return saveResult.IsSuccess 
                ? Result.Success() 
                : Result.Error(new ErrorList(new[] { $"Failed to save execution completion: {saveResult.Errors.FirstOrDefault()}" }, null));
        }
        catch (Exception ex)
        {
            return Result.Error(new ErrorList(new[] { $"Error completing execution: {ex.Message}" }, null));
        }
    }

    public async Task<Result> FailExecutionAsync(Guid id, string? errorMessage = null)
    {
        try
        {
            var executionResult = await GetByIdAsync(id);
            if (!executionResult.IsSuccess) return Result.NotFound($"Execution {id} not found");

            var execution = executionResult.Value;
            execution.Status = "Failed";
            execution.CompletedAt = DateTime.UtcNow;
            if (!string.IsNullOrEmpty(errorMessage))
                execution.ErrorMessage = errorMessage;

            var updateResult = await UpdateAsync(execution);
            if (!updateResult.IsSuccess) 
                return Result.Error(new ErrorList(new[] { $"Failed to update execution: {updateResult.Errors.FirstOrDefault()}" }, null));
            
            var saveResult = await SaveChangesAsync();
            return saveResult.IsSuccess 
                ? Result.Success() 
                : Result.Error(new ErrorList(new[] { $"Failed to save execution failure: {saveResult.Errors.FirstOrDefault()}" }, null));
        }
        catch (Exception ex)
        {
            return Result.Error(new ErrorList(new[] { $"Error failing execution: {ex.Message}" }, null));
        }
    }

    public async Task<Result> UpdateTokenUsageAsync(Guid id, int inputTokens, int outputTokens, decimal cost)
    {
        try
        {
            if (inputTokens < 0) return Result.Invalid(new ValidationError { ErrorMessage = "Input tokens cannot be negative" });
            if (outputTokens < 0) return Result.Invalid(new ValidationError { ErrorMessage = "Output tokens cannot be negative" });
            if (cost < 0) return Result.Invalid(new ValidationError { ErrorMessage = "Cost cannot be negative" });

            var executionResult = await GetByIdAsync(id);
            if (!executionResult.IsSuccess) return Result.NotFound($"Execution {id} not found");

            var execution = executionResult.Value;
            execution.InputTokens = inputTokens;
            execution.OutputTokens = outputTokens;
            execution.ExecutionCost = cost;

            var updateResult = await UpdateAsync(execution);
            if (!updateResult.IsSuccess) 
                return Result.Error(new ErrorList(new[] { $"Failed to update execution: {updateResult.Errors.FirstOrDefault()}" }, null));
            
            var saveResult = await SaveChangesAsync();
            return saveResult.IsSuccess 
                ? Result.Success() 
                : Result.Error(new ErrorList(new[] { $"Failed to save token usage: {saveResult.Errors.FirstOrDefault()}" }, null));
        }
        catch (Exception ex)
        {
            return Result.Error(new ErrorList(new[] { $"Error updating token usage: {ex.Message}" }, null));
        }
    }

    public async Task<Result> UpdateProgressAsync(Guid id, int progressPercentage)
    {
        try
        {
            if (progressPercentage < 0 || progressPercentage > 100)
                return Result.Invalid(new ValidationError { ErrorMessage = "Progress percentage must be between 0 and 100" });

            // Progress tracking is handled through status updates since AgentExecution doesn't have ProgressPercentage property
            // This method is kept for interface compatibility but doesn't update any specific progress field
            var executionResult = await GetByIdAsync(id);
            if (!executionResult.IsSuccess) return Result.NotFound($"Execution {id} not found");

            // Could potentially update a metadata field or status based on progress
            // For now, just return success
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Error(new ErrorList(new[] { $"Error updating progress: {ex.Message}" }, null));
        }
    }

    // Search and filtering
    public async Task<Result<IEnumerable<AgentExecution>>> SearchExecutionsAsync(string searchTerm)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return Result.Invalid(new ValidationError { ErrorMessage = "Search term cannot be null or empty" });

            var lowerSearchTerm = searchTerm.ToLower();
            var executions = await _dbSet
                .Where(ae => ae.AgentName.ToLower().Contains(lowerSearchTerm) ||
                            ae.AgentType.ToLower().Contains(lowerSearchTerm) ||
                            (ae.ErrorMessage != null && ae.ErrorMessage.ToLower().Contains(lowerSearchTerm)))
                .OrderByDescending(ae => ae.StartedAt)
                .ToListAsync();
            return Result.Success(executions.AsEnumerable());
        }
        catch (Exception ex)
        {
            return Result.Error(new ErrorList(new[] { $"Error searching executions: {ex.Message}" }, null));
        }
    }

    public async Task<Result<IEnumerable<AgentExecution>>> GetExecutionsWithHighTokenUsageAsync(int threshold)
    {
        try
        {
            if (threshold < 0) return Result.Invalid(new ValidationError { ErrorMessage = "Threshold cannot be negative" });

            var executions = await _dbSet
                .Where(ae => ae.InputTokens.HasValue && ae.OutputTokens.HasValue &&
                            (ae.InputTokens.Value + ae.OutputTokens.Value) > threshold)
                .OrderByDescending(ae => ae.InputTokens!.Value + ae.OutputTokens!.Value)
                .ToListAsync();
            return Result.Success(executions.AsEnumerable());
        }
        catch (Exception ex)
        {
            return Result.Error(new ErrorList(new[] { $"Error retrieving executions with high token usage: {ex.Message}" }, null));
        }
    }

    public async Task<Result<IEnumerable<AgentExecution>>> GetExecutionsWithHighCostAsync(decimal threshold)
    {
        try
        {
            if (threshold < 0) return Result.Invalid(new ValidationError { ErrorMessage = "Threshold cannot be negative" });

            var executions = await _dbSet
                .Where(ae => ae.ExecutionCost.HasValue && ae.ExecutionCost.Value > threshold)
                .OrderByDescending(ae => ae.ExecutionCost!.Value)
                .ToListAsync();
            return Result.Success(executions.AsEnumerable());
        }
        catch (Exception ex)
        {
            return Result.Error(new ErrorList(new[] { $"Error retrieving executions with high cost: {ex.Message}" }, null));
        }
    }

    public async Task<Result<IEnumerable<AgentExecution>>> GetLongRunningExecutionsAsync(TimeSpan threshold)
    {
        try
        {
            var cutoffTime = DateTime.UtcNow.Subtract(threshold);
            var executions = await _dbSet
                .Where(ae => ae.StartedAt <= cutoffTime && 
                            (ae.Status == "Running" || ae.Status == "InProgress"))
                .OrderBy(ae => ae.StartedAt)
                .ToListAsync();
            return Result.Success(executions.AsEnumerable());
        }
        catch (Exception ex)
        {
            return Result.Error(new ErrorList(new[] { $"Error retrieving long running executions: {ex.Message}" }, null));
        }
    }

    public async Task<Result<IEnumerable<AgentExecution>>> GetExecutionsStartedBetweenAsync(DateTime startDate, DateTime endDate)
    {
        try
        {
            if (startDate > endDate) return Result.Invalid(new ValidationError { ErrorMessage = "Start date cannot be after end date" });

            var executions = await _dbSet
                .Where(ae => ae.StartedAt >= startDate && ae.StartedAt <= endDate)
                .OrderByDescending(ae => ae.StartedAt)
                .ToListAsync();
            return Result.Success(executions.AsEnumerable());
        }
        catch (Exception ex)
        {
            return Result.Error(new ErrorList(new[] { $"Error retrieving executions started between dates: {ex.Message}" }, null));
        }
    }

    public async Task<Result<IEnumerable<AgentExecution>>> GetRecentExecutionsAsync(int count = 50)
    {
        try
        {
            if (count <= 0) return Result.Invalid(new ValidationError { ErrorMessage = "Count must be greater than 0" });

            var executions = await _dbSet
                .OrderByDescending(ae => ae.StartedAt)
                .Take(count)
                .ToListAsync();
            return Result.Success(executions.AsEnumerable());
        }
        catch (Exception ex)
        {
            return Result.Error(new ErrorList(new[] { $"Error retrieving recent executions: {ex.Message}" }, null));
        }
    }

    public async Task<Result<IEnumerable<AgentExecution>>> GetExecutionsWithErrorsAsync()
    {
        try
        {
            var executions = await _dbSet
                .Where(ae => !string.IsNullOrEmpty(ae.ErrorMessage))
                .OrderByDescending(ae => ae.StartedAt)
                .ToListAsync();
            return Result.Success(executions.AsEnumerable());
        }
        catch (Exception ex)
        {
            return Result.Error(new ErrorList(new[] { $"Error retrieving executions with errors: {ex.Message}" }, null));
        }
    }

    // Model and provider specific operations
    public async Task<Result<IEnumerable<AgentExecution>>> GetByModelAsync(string modelName)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(modelName))
                return Result.Invalid(new ValidationError { ErrorMessage = "Model name cannot be null or empty" });

            var executions = await _dbSet
                .Where(ae => ae.ModelUsed == modelName)
                .OrderByDescending(ae => ae.StartedAt)
                .ToListAsync();
            return Result.Success(executions.AsEnumerable());
        }
        catch (Exception ex)
        {
            return Result.Error(new ErrorList(new[] { $"Error retrieving executions by model {modelName}: {ex.Message}" }, null));
        }
    }

    public async Task<Result<IEnumerable<AgentExecution>>> GetByProviderAsync(string provider)
    {
        try
        {
            // Provider property doesn't exist in AgentExecution entity
            // This method is kept for interface compatibility but returns empty result
            return Result.Success(Enumerable.Empty<AgentExecution>());
        }
        catch (Exception ex)
        {
            return Result.Error(new ErrorList(new[] { $"Error retrieving executions by provider: {ex.Message}" }, null));
        }
    }

    public async Task<Result<IEnumerable<(string Model, int Count, decimal TotalCost)>>> GetModelUsageStatsAsync()
    {
        try
        {
            var stats = await _dbSet
                .Where(ae => !string.IsNullOrEmpty(ae.ModelUsed))
                .GroupBy(ae => ae.ModelUsed!)
                .Select(g => new
                {
                    Model = g.Key,
                    Count = g.Count(),
                    TotalCost = g.Where(ae => ae.ExecutionCost.HasValue).Sum(ae => ae.ExecutionCost!.Value)
                })
                .OrderByDescending(x => x.Count)
                .Select(x => ValueTuple.Create(x.Model, x.Count, x.TotalCost))
                .ToListAsync();
            return Result.Success(stats.AsEnumerable());
        }
        catch (Exception ex)
        {
            return Result.Error(new ErrorList(new[] { $"Error retrieving model usage statistics: {ex.Message}" }, null));
        }
    }

    public async Task<Result<IEnumerable<(string Provider, int Count, decimal TotalCost)>>> GetProviderUsageStatsAsync()
    {
        try
        {
            // Provider property doesn't exist in AgentExecution entity
            // This method is kept for interface compatibility but returns empty result
            return Result.Success(Enumerable.Empty<(string, int, decimal)>());
        }
        catch (Exception ex)
        {
            return Result.Error(new ErrorList(new[] { $"Error retrieving provider usage statistics: {ex.Message}" }, null));
        }
    }
}