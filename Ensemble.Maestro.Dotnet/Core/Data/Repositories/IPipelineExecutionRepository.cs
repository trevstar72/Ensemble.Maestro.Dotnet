using Ensemble.Maestro.Dotnet.Core.Data.Entities;
using Ardalis.Result;

namespace Ensemble.Maestro.Dotnet.Core.Data.Repositories;

/// <summary>
/// Repository interface for PipelineExecution entities with specialized operations
/// </summary>
public interface IPipelineExecutionRepository : IRepository<PipelineExecution>
{
    // Query operations
    Task<Result<IEnumerable<PipelineExecution>>> GetByProjectIdAsync(Guid projectId);
    Task<Result<IEnumerable<PipelineExecution>>> GetByStatusAsync(string status);
    Task<Result<IEnumerable<PipelineExecution>>> GetActiveExecutionsAsync();
    Task<Result<IEnumerable<PipelineExecution>>> GetCompletedExecutionsAsync();
    Task<Result<IEnumerable<PipelineExecution>>> GetFailedExecutionsAsync();
    Task<Result<PipelineExecution>> GetLatestForProjectAsync(Guid projectId);

    // Complex queries with related data
    Task<Result<PipelineExecution>> GetWithAllRelatedDataAsync(Guid id);
    Task<Result<PipelineExecution>> GetWithStageExecutionsAsync(Guid id);
    Task<Result<PipelineExecution>> GetWithOrchestrationResultsAsync(Guid id);
    Task<Result<PipelineExecution>> GetWithAgentExecutionsAsync(Guid id);
    Task<Result<IEnumerable<PipelineExecution>>> GetExecutionsWithStagesAsync(Guid projectId);

    // Analytics and statistics
    Task<Result<int>> GetExecutionCountByStatusAsync(string status);
    Task<Result<decimal>> GetAverageExecutionTimeAsync();
    Task<Result<(int Total, int Running, int Completed, int Failed)>> GetExecutionStatisticsAsync();
    Task<Result<(int Total, int Running, int Completed, int Failed)>> GetExecutionStatisticsForProjectAsync(Guid projectId);
    Task<Result<IEnumerable<(string Status, int Count)>>> GetStatusDistributionAsync();
    Task<Result<IEnumerable<(DateTime Date, int Count)>>> GetExecutionTrendAsync(int days = 30);

    // Performance metrics
    Task<Result<decimal>> GetAverageTokenUsageAsync();
    Task<Result<decimal>> GetAverageCostAsync();
    Task<Result<TimeSpan>> GetAverageExecutionDurationAsync();
    Task<Result<(decimal TotalCost, int TotalTokens, TimeSpan TotalDuration)>> GetAggregateMetricsAsync(Guid projectId);

    // Business operations
    Task<Result<PipelineExecution>> StartExecutionAsync(Guid projectId, string orchestrationStrategy);
    Task<Result> UpdateExecutionStatusAsync(Guid id, string status);
    Task<Result> UpdateProgressAsync(Guid id, int progressPercentage);
    Task<Result> CompleteExecutionAsync(Guid id, bool success = true);
    Task<Result> FailExecutionAsync(Guid id, string? errorMessage = null);

    // Stage operations
    Task<Result<IEnumerable<StageExecution>>> GetStagesForExecutionAsync(Guid executionId);
    Task<Result<StageExecution>> GetCurrentStageAsync(Guid executionId);
    Task<Result<StageExecution>> GetStageByNameAsync(Guid executionId, string stageName);

    // Time-based queries
    Task<Result<IEnumerable<PipelineExecution>>> GetExecutionsStartedBetweenAsync(DateTime startDate, DateTime endDate);
    Task<Result<IEnumerable<PipelineExecution>>> GetLongRunningExecutionsAsync(TimeSpan threshold);
    Task<Result<IEnumerable<PipelineExecution>>> GetRecentExecutionsAsync(int count = 20);

    // Orchestration pattern queries
    Task<Result<IEnumerable<PipelineExecution>>> GetByOrchestrationStrategyAsync(string strategy);
    Task<Result<IEnumerable<(string Strategy, int Count, decimal AvgDuration)>>> GetOrchestrationEfficiencyAsync();
}