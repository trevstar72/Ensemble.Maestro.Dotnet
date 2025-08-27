using Ensemble.Maestro.Dotnet.Core.Data.Entities;
using Ardalis.Result;

namespace Ensemble.Maestro.Dotnet.Core.Data.Repositories;

/// <summary>
/// Repository interface for Project entities with specialized operations
/// </summary>
public interface IProjectRepository : IRepository<Project>
{
    // Specialized query operations
    Task<Result<Project>> GetByNameAsync(string name);
    Task<Result<IEnumerable<Project>>> GetByStatusAsync(string status);
    Task<Result<IEnumerable<Project>>> GetByTargetLanguageAsync(string targetLanguage);
    Task<Result<IEnumerable<Project>>> GetByComplexityScoreAsync(int minScore, int maxScore);
    Task<Result<IEnumerable<Project>>> GetActiveProjectsAsync();
    Task<Result<IEnumerable<Project>>> GetRecentProjectsAsync(int count = 10);

    // Complex queries with related data
    Task<Result<Project>> GetWithAllRelatedDataAsync(Guid id);
    Task<Result<Project>> GetWithPipelineExecutionsAsync(Guid id);
    Task<Result<Project>> GetWithFilesAsync(Guid id);
    Task<Result<Project>> GetWithModulesAsync(Guid id);
    Task<Result<Project>> GetWithAgentExecutionsAsync(Guid id);

    // Statistics and analytics
    Task<Result<int>> GetProjectCountByStatusAsync(string status);
    Task<Result<decimal>> GetAverageComplexityScoreAsync();
    Task<Result<(int Total, int Completed, int InProgress, int Failed)>> GetProjectStatisticsAsync();
    Task<Result<IEnumerable<(string Status, int Count)>>> GetProjectStatusDistributionAsync();
    Task<Result<IEnumerable<(string Language, int Count)>>> GetProjectLanguageDistributionAsync();

    // Business operations
    Task<Result<Project>> CreateProjectAsync(string name, string requirements, string? targetLanguage = null);
    Task<Result> UpdateProjectStatusAsync(Guid id, string status);
    Task<Result> UpdateProjectProgressAsync(Guid id, int progressPercentage);
    Task<Result> CompleteProjectAsync(Guid id);
    Task<Result> ArchiveProjectAsync(Guid id);

    // Search and filtering
    Task<Result<IEnumerable<Project>>> SearchProjectsAsync(string searchTerm);
    Task<Result<IEnumerable<Project>>> GetProjectsByTagsAsync(string[] tags);
    Task<Result<IEnumerable<Project>>> GetProjectsCreatedBetweenAsync(DateTime startDate, DateTime endDate);
    Task<Result<IEnumerable<Project>>> GetProjectsUpdatedSinceAsync(DateTime since);

    // File operations
    Task<Result<int>> GetFileCountForProjectAsync(Guid projectId);
    Task<Result<long>> GetTotalFileSizeForProjectAsync(Guid projectId);
    Task<Result<IEnumerable<ProjectFile>>> GetGeneratedFilesAsync(Guid projectId);
    Task<Result<IEnumerable<ProjectFile>>> GetFilesByTypeAsync(Guid projectId, string contentType);

    // Pipeline operations
    Task<Result<PipelineExecution>> GetLatestPipelineExecutionAsync(Guid projectId);
    Task<Result<IEnumerable<PipelineExecution>>> GetPipelineExecutionHistoryAsync(Guid projectId);
    Task<Result<bool>> HasActivePipelineExecutionAsync(Guid projectId);
}