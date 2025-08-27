using Ensemble.Maestro.Dotnet.Core.Data.Entities;
using Ardalis.Result;

namespace Ensemble.Maestro.Dotnet.Core.Data.Repositories;

/// <summary>
/// Repository interface for AgentExecution entities with specialized operations
/// </summary>
public interface IAgentExecutionRepository : IRepository<AgentExecution>
{
    // Query operations
    Task<Result<IEnumerable<AgentExecution>>> GetByProjectIdAsync(Guid projectId);
    Task<Result<IEnumerable<AgentExecution>>> GetByPipelineExecutionIdAsync(Guid pipelineExecutionId);
    Task<Result<IEnumerable<AgentExecution>>> GetByStageExecutionIdAsync(Guid stageExecutionId);
    Task<Result<IEnumerable<AgentExecution>>> GetByAgentTypeAsync(string agentType);
    Task<Result<IEnumerable<AgentExecution>>> GetByStatusAsync(string status);
    Task<Result<IEnumerable<AgentExecution>>> GetActiveExecutionsAsync();
    Task<Result<IEnumerable<AgentExecution>>> GetCompletedExecutionsAsync();
    Task<Result<IEnumerable<AgentExecution>>> GetFailedExecutionsAsync();

    // Hierarchical operations
    Task<Result<IEnumerable<AgentExecution>>> GetChildExecutionsAsync(Guid parentId);
    Task<Result<AgentExecution>> GetParentExecutionAsync(Guid childId);
    Task<Result<IEnumerable<AgentExecution>>> GetExecutionHierarchyAsync(Guid rootId);
    Task<Result<int>> GetHierarchyDepthAsync(Guid executionId);

    // Complex queries with related data
    Task<Result<AgentExecution>> GetWithMessagesAsync(Guid id);
    Task<Result<AgentExecution>> GetWithAllRelatedDataAsync(Guid id);
    Task<Result<IEnumerable<AgentExecution>>> GetExecutionsWithMessagesAsync(Guid projectId);

    // Analytics and statistics
    Task<Result<int>> GetExecutionCountByAgentTypeAsync(string agentType);
    Task<Result<int>> GetExecutionCountByStatusAsync(string status);
    Task<Result<(int Total, int Running, int Completed, int Failed)>> GetExecutionStatisticsAsync();
    Task<Result<(int Total, int Running, int Completed, int Failed)>> GetExecutionStatisticsForProjectAsync(Guid projectId);
    Task<Result<(int Total, int Running, int Completed, int Failed)>> GetExecutionStatisticsForPipelineAsync(Guid pipelineExecutionId);
    Task<Result<IEnumerable<(string AgentType, int Count)>>> GetAgentTypeDistributionAsync();
    Task<Result<IEnumerable<(string Status, int Count)>>> GetStatusDistributionAsync();

    // Performance metrics
    Task<Result<decimal>> GetAverageExecutionTimeByAgentTypeAsync(string agentType);
    Task<Result<decimal>> GetAverageTokenUsageByAgentTypeAsync(string agentType);
    Task<Result<decimal>> GetAverageCostByAgentTypeAsync(string agentType);
    Task<Result<(decimal TotalCost, int TotalInputTokens, int TotalOutputTokens)>> GetAggregateMetricsAsync(Guid projectId);
    Task<Result<(decimal TotalCost, int TotalInputTokens, int TotalOutputTokens)>> GetAggregateMetricsForPipelineAsync(Guid pipelineExecutionId);
    Task<Result<IEnumerable<(string AgentType, decimal AvgCost, int AvgTokens, decimal AvgDuration)>>> GetAgentEfficiencyMetricsAsync();

    // Message operations
    Task<Result<IEnumerable<AgentMessage>>> GetMessagesForExecutionAsync(Guid executionId);
    Task<Result<int>> GetMessageCountForExecutionAsync(Guid executionId);
    Task<Result<AgentMessage>> GetLatestMessageAsync(Guid executionId);
    Task<Result<IEnumerable<AgentMessage>>> GetConversationHistoryAsync(Guid executionId);

    // Business operations
    Task<Result<AgentExecution>> StartExecutionAsync(Guid projectId, Guid pipelineExecutionId, string agentType, string agentName);
    Task<Result<AgentExecution>> StartChildExecutionAsync(Guid parentId, string agentType, string agentName);
    Task<Result> UpdateExecutionStatusAsync(Guid id, string status);
    Task<Result> CompleteExecutionAsync(Guid id, bool success = true);
    Task<Result> FailExecutionAsync(Guid id, string? errorMessage = null);
    Task<Result> UpdateTokenUsageAsync(Guid id, int inputTokens, int outputTokens, decimal cost);
    Task<Result> UpdateProgressAsync(Guid id, int progressPercentage);

    // Search and filtering
    Task<Result<IEnumerable<AgentExecution>>> SearchExecutionsAsync(string searchTerm);
    Task<Result<IEnumerable<AgentExecution>>> GetExecutionsWithHighTokenUsageAsync(int threshold);
    Task<Result<IEnumerable<AgentExecution>>> GetExecutionsWithHighCostAsync(decimal threshold);
    Task<Result<IEnumerable<AgentExecution>>> GetLongRunningExecutionsAsync(TimeSpan threshold);
    Task<Result<IEnumerable<AgentExecution>>> GetExecutionsStartedBetweenAsync(DateTime startDate, DateTime endDate);
    Task<Result<IEnumerable<AgentExecution>>> GetRecentExecutionsAsync(int count = 50);
    Task<Result<IEnumerable<AgentExecution>>> GetExecutionsWithErrorsAsync();

    // Model and provider specific operations
    Task<Result<IEnumerable<AgentExecution>>> GetByModelAsync(string modelName);
    Task<Result<IEnumerable<AgentExecution>>> GetByProviderAsync(string provider);
    Task<Result<IEnumerable<(string Model, int Count, decimal TotalCost)>>> GetModelUsageStatsAsync();
    Task<Result<IEnumerable<(string Provider, int Count, decimal TotalCost)>>> GetProviderUsageStatsAsync();
}