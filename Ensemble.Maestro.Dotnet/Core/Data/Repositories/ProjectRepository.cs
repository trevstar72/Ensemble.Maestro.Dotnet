using Ensemble.Maestro.Dotnet.Core.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Ardalis.Result;

namespace Ensemble.Maestro.Dotnet.Core.Data.Repositories;

/// <summary>
/// Repository implementation for Project entities with specialized operations
/// </summary>
public class ProjectRepository : Repository<Project>, IProjectRepository
{
    public ProjectRepository(MaestroDbContext context) : base(context)
    {
    }

    // Specialized query operations
    public async Task<Result<Project>> GetByNameAsync(string name)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(name))
                return Result.Invalid(new[] { new ValidationError { ErrorMessage = "Name cannot be null or empty" } });

            var project = await _dbSet.FirstOrDefaultAsync(p => p.Name == name);
            return project != null 
                ? Result.Success(project) 
                : Result.NotFound($"Project with name '{name}' not found");
        }
        catch (Exception ex)
        {
            return Result.Error(new ErrorList(new[] { $"Error retrieving project by name '{name}': {ex.Message}" }, null));
        }
    }

    public async Task<Result<IEnumerable<Project>>> GetByStatusAsync(string status)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(status))
                return Result.Invalid(new[] { new ValidationError { ErrorMessage = "Status cannot be null or empty" } });

            var projects = await _dbSet.Where(p => p.Status == status).ToListAsync();
            return Result.Success(projects.AsEnumerable());
        }
        catch (Exception ex)
        {
            return Result.Error(new ErrorList(new[] { $"Error retrieving projects by status '{status}': {ex.Message}" }, null));
        }
    }

    public async Task<Result<IEnumerable<Project>>> GetByTargetLanguageAsync(string targetLanguage)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(targetLanguage))
                return Result.Invalid(new[] { new ValidationError { ErrorMessage = "Target language cannot be null or empty" } });

            var projects = await _dbSet.Where(p => p.TargetLanguage == targetLanguage).ToListAsync();
            return Result.Success(projects.AsEnumerable());
        }
        catch (Exception ex)
        {
            return Result.Error(new ErrorList(new[] { $"Error retrieving projects by target language '{targetLanguage}': {ex.Message}" }, null));
        }
    }

    public async Task<Result<IEnumerable<Project>>> GetByComplexityScoreAsync(int minScore, int maxScore)
    {
        try
        {
            if (minScore < 0 || maxScore < 0)
                return Result.Invalid(new[] { new ValidationError { ErrorMessage = "Complexity scores cannot be negative" } });
            if (minScore > maxScore)
                return Result.Invalid(new[] { new ValidationError { ErrorMessage = "Minimum score cannot be greater than maximum score" } });

            var projects = await _dbSet
                .Where(p => p.ComplexityScore.HasValue && 
                           p.ComplexityScore.Value >= minScore && 
                           p.ComplexityScore.Value <= maxScore)
                .ToListAsync();
            return Result.Success(projects.AsEnumerable());
        }
        catch (Exception ex)
        {
            return Result.Error(new ErrorList(new[] { $"Error retrieving projects by complexity score ({minScore}-{maxScore}): {ex.Message}" }, null));
        }
    }

    public async Task<Result<IEnumerable<Project>>> GetActiveProjectsAsync()
    {
        try
        {
            var projects = await _dbSet
                .Where(p => p.Status != "Completed" && p.Status != "Cancelled" && p.Status != "Archived")
                .OrderByDescending(p => p.UpdatedAt)
                .ToListAsync();
            return Result.Success(projects.AsEnumerable());
        }
        catch (Exception ex)
        {
            return Result.Error(new ErrorList(new[] { $"Error retrieving active projects: {ex.Message}" }, null));
        }
    }

    public async Task<Result<IEnumerable<Project>>> GetRecentProjectsAsync(int count = 10)
    {
        try
        {
            if (count <= 0)
                return Result.Invalid(new[] { new ValidationError { ErrorMessage = "Count must be greater than 0" } });

            var projects = await _dbSet
                .OrderByDescending(p => p.CreatedAt)
                .Take(count)
                .ToListAsync();
            return Result.Success(projects.AsEnumerable());
        }
        catch (Exception ex)
        {
            return Result.Error(new ErrorList(new[] { $"Error retrieving recent projects: {ex.Message}" }, null));
        }
    }

    // Complex queries with related data
    public async Task<Result<Project>> GetWithAllRelatedDataAsync(Guid id)
    {
        try
        {
            var project = await _dbSet
                .Include(p => p.PipelineExecutions)
                    .ThenInclude(pe => pe.StageExecutions)
                .Include(p => p.Files)
                .Include(p => p.Modules)
                .Include(p => p.AgentExecutions)
                    .ThenInclude(ae => ae.Messages)
                .FirstOrDefaultAsync(p => p.Id == id);

            return project != null 
                ? Result.Success(project) 
                : Result.NotFound($"Project with ID {id} not found");
        }
        catch (Exception ex)
        {
            return Result.Error(new ErrorList(new[] { $"Error retrieving project {id} with all related data: {ex.Message}" }, null));
        }
    }

    public async Task<Result<Project>> GetWithPipelineExecutionsAsync(Guid id)
    {
        try
        {
            var project = await _dbSet
                .Include(p => p.PipelineExecutions)
                    .ThenInclude(pe => pe.StageExecutions)
                .Include(p => p.PipelineExecutions)
                    .ThenInclude(pe => pe.OrchestrationResults)
                .FirstOrDefaultAsync(p => p.Id == id);

            return project != null 
                ? Result.Success(project) 
                : Result.NotFound($"Project with ID {id} not found");
        }
        catch (Exception ex)
        {
            return Result.Error(new ErrorList(new[] { $"Error retrieving project {id} with pipeline executions: {ex.Message}" }, null));
        }
    }

    public async Task<Result<Project>> GetWithFilesAsync(Guid id)
    {
        try
        {
            var project = await _dbSet
                .Include(p => p.Files.Where(f => f.IsActive))
                .FirstOrDefaultAsync(p => p.Id == id);

            return project != null 
                ? Result.Success(project) 
                : Result.NotFound($"Project with ID {id} not found");
        }
        catch (Exception ex)
        {
            return Result.Error(new ErrorList(new[] { $"Error retrieving project {id} with files: {ex.Message}" }, null));
        }
    }

    public async Task<Result<Project>> GetWithModulesAsync(Guid id)
    {
        try
        {
            var project = await _dbSet
                .Include(p => p.Modules.Where(m => m.IsActive))
                    .ThenInclude(m => m.Files)
                .FirstOrDefaultAsync(p => p.Id == id);

            return project != null 
                ? Result.Success(project) 
                : Result.NotFound($"Project with ID {id} not found");
        }
        catch (Exception ex)
        {
            return Result.Error(new ErrorList(new[] { $"Error retrieving project {id} with modules: {ex.Message}" }, null));
        }
    }

    public async Task<Result<Project>> GetWithAgentExecutionsAsync(Guid id)
    {
        try
        {
            var project = await _dbSet
                .Include(p => p.AgentExecutions.OrderByDescending(ae => ae.StartedAt))
                    .ThenInclude(ae => ae.Messages)
                .FirstOrDefaultAsync(p => p.Id == id);

            return project != null 
                ? Result.Success(project) 
                : Result.NotFound($"Project with ID {id} not found");
        }
        catch (Exception ex)
        {
            return Result.Error(new ErrorList(new[] { $"Error retrieving project {id} with agent executions: {ex.Message}" }, null));
        }
    }

    // Statistics and analytics
    public async Task<Result<int>> GetProjectCountByStatusAsync(string status)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(status))
                return Result<int>.Invalid(new ValidationError { ErrorMessage = "Status cannot be null or empty" });

            var count = await _dbSet.CountAsync(p => p.Status == status);
            return Result.Success(count);
        }
        catch (Exception ex)
        {
            return Result.Error(new ErrorList(new[] { $"Error counting projects by status '{status}': {ex.Message}" }, null));
        }
    }

    public async Task<Result<decimal>> GetAverageComplexityScoreAsync()
    {
        try
        {
            var scores = await _dbSet
                .Where(p => p.ComplexityScore.HasValue)
                .Select(p => p.ComplexityScore!.Value)
                .ToListAsync();
            
            var average = scores.Any() ? (decimal)scores.Average() : 0;
            return Result.Success(average);
        }
        catch (Exception ex)
        {
            return Result.Error(new ErrorList(new[] { $"Error calculating average complexity score: {ex.Message}" }, null));
        }
    }

    public async Task<Result<(int Total, int Completed, int InProgress, int Failed)>> GetProjectStatisticsAsync()
    {
        try
        {
            var projects = await _dbSet.ToListAsync();
            var total = projects.Count;
            var completed = projects.Count(p => p.Status == "Completed");
            var inProgress = projects.Count(p => p.Status == "InProgress" || p.Status == "Running");
            var failed = projects.Count(p => p.Status == "Failed" || p.Status == "Error");

            return Result.Success((total, completed, inProgress, failed));
        }
        catch (Exception ex)
        {
            return Result.Error(new ErrorList(new[] { $"Error retrieving project statistics: {ex.Message}" }, null));
        }
    }

    public async Task<Result<IEnumerable<(string Status, int Count)>>> GetProjectStatusDistributionAsync()
    {
        try
        {
            var distribution = await _dbSet
                .GroupBy(p => p.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Select(x => ValueTuple.Create(x.Status, x.Count))
                .ToListAsync();
            return Result.Success(distribution.AsEnumerable());
        }
        catch (Exception ex)
        {
            return Result.Error(new ErrorList(new[] { $"Error retrieving project status distribution: {ex.Message}" }, null));
        }
    }

    public async Task<Result<IEnumerable<(string Language, int Count)>>> GetProjectLanguageDistributionAsync()
    {
        try
        {
            var distribution = await _dbSet
                .Where(p => !string.IsNullOrEmpty(p.TargetLanguage))
                .GroupBy(p => p.TargetLanguage!)
                .Select(g => new { Language = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Select(x => ValueTuple.Create(x.Language, x.Count))
                .ToListAsync();
            return Result.Success(distribution.AsEnumerable());
        }
        catch (Exception ex)
        {
            return Result.Error(new ErrorList(new[] { $"Error retrieving project language distribution: {ex.Message}" }, null));
        }
    }

    // Business operations
    public async Task<Result<Project>> CreateProjectAsync(string name, string requirements, string? targetLanguage = null)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(name))
                return Result.Invalid(new[] { new ValidationError { ErrorMessage = "Project name cannot be null or empty" } });
            if (string.IsNullOrWhiteSpace(requirements))
                return Result.Invalid(new[] { new ValidationError { ErrorMessage = "Project requirements cannot be null or empty" } });

            // Check if project with same name already exists
            var existing = await _dbSet.FirstOrDefaultAsync(p => p.Name == name);
            if (existing != null)
                return Result.Invalid(new[] { new ValidationError { ErrorMessage = $"Project with name '{name}' already exists" } });

            var project = new Project
            {
                Name = name,
                Requirements = requirements,
                TargetLanguage = targetLanguage,
                Status = "Created",
                Priority = "Medium",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var addResult = await AddAsync(project);
            if (!addResult.IsSuccess)
                return Result.Error(new ErrorList(new[] { $"Failed to add project: {addResult.Errors.FirstOrDefault()}" }, null));

            var saveResult = await SaveChangesAsync();
            return saveResult.IsSuccess 
                ? Result.Success(project)
                : Result.Error(new ErrorList(new[] { $"Failed to save project: {saveResult.Errors.FirstOrDefault()}" }, null));
        }
        catch (Exception ex)
        {
            return Result.Error(new ErrorList(new[] { $"Error creating project: {ex.Message}" }, null));
        }
    }

    public async Task<Result> UpdateProjectStatusAsync(Guid id, string status)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(status))
                return Result.Invalid(new[] { new ValidationError { ErrorMessage = "Status cannot be null or empty" } });

            var projectResult = await GetByIdAsync(id);
            if (!projectResult.IsSuccess) return Result.NotFound($"Project {id} not found");

            var project = projectResult.Value;
            project.Status = status;
            project.UpdatedAt = DateTime.UtcNow;

            if (status == "Completed")
                project.CompletedAt = DateTime.UtcNow;

            var updateResult = await UpdateAsync(project);
            if (!updateResult.IsSuccess)
                return Result.Error(new ErrorList(new[] { $"Failed to update project: {updateResult.Errors.FirstOrDefault()}" }, null));

            var saveResult = await SaveChangesAsync();
            return saveResult.IsSuccess 
                ? Result.Success() 
                : Result.Error(new ErrorList(new[] { $"Failed to save project status: {saveResult.Errors.FirstOrDefault()}" }, null));
        }
        catch (Exception ex)
        {
            return Result.Error(new ErrorList(new[] { $"Error updating project status: {ex.Message}" }, null));
        }
    }

    public async Task<Result> UpdateProjectProgressAsync(Guid id, int progressPercentage)
    {
        try
        {
            if (progressPercentage < 0 || progressPercentage > 100)
                return Result.Invalid(new[] { new ValidationError { ErrorMessage = "Progress percentage must be between 0 and 100" } });

            var projectResult = await GetByIdAsync(id);
            if (!projectResult.IsSuccess) return Result.NotFound($"Project {id} not found");

            var project = projectResult.Value;
            project.UpdatedAt = DateTime.UtcNow;
            // Note: Progress is calculated based on pipeline executions, files, etc.
            // This is a placeholder implementation

            var updateResult = await UpdateAsync(project);
            if (!updateResult.IsSuccess)
                return Result.Error(new ErrorList(new[] { $"Failed to update project: {updateResult.Errors.FirstOrDefault()}" }, null));

            var saveResult = await SaveChangesAsync();
            return saveResult.IsSuccess 
                ? Result.Success() 
                : Result.Error(new ErrorList(new[] { $"Failed to save project progress: {saveResult.Errors.FirstOrDefault()}" }, null));
        }
        catch (Exception ex)
        {
            return Result.Error(new ErrorList(new[] { $"Error updating project progress: {ex.Message}" }, null));
        }
    }

    public async Task<Result> CompleteProjectAsync(Guid id)
    {
        return await UpdateProjectStatusAsync(id, "Completed");
    }

    public async Task<Result> ArchiveProjectAsync(Guid id)
    {
        return await UpdateProjectStatusAsync(id, "Archived");
    }

    // Search and filtering
    public async Task<Result<IEnumerable<Project>>> SearchProjectsAsync(string searchTerm)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return Result.Invalid(new[] { new ValidationError { ErrorMessage = "Search term cannot be null or empty" } });

            var lowerSearchTerm = searchTerm.ToLower();
            var projects = await _dbSet
                .Where(p => p.Name.ToLower().Contains(lowerSearchTerm) ||
                           p.Requirements.ToLower().Contains(lowerSearchTerm) ||
                           (p.Charter != null && p.Charter.ToLower().Contains(lowerSearchTerm)))
                .OrderByDescending(p => p.UpdatedAt)
                .ToListAsync();
            return Result.Success(projects.AsEnumerable());
        }
        catch (Exception ex)
        {
            return Result.Error(new ErrorList(new[] { $"Error searching projects: {ex.Message}" }, null));
        }
    }

    public async Task<Result<IEnumerable<Project>>> GetProjectsByTagsAsync(string[] tags)
    {
        try
        {
            if (tags == null || !tags.Any())
                return Result.Invalid(new[] { new ValidationError { ErrorMessage = "Tags array cannot be null or empty" } });

            var projects = await _dbSet
                .Where(p => p.Tags != null && tags.Any(tag => p.Tags.Contains(tag)))
                .OrderByDescending(p => p.UpdatedAt)
                .ToListAsync();
            return Result.Success(projects.AsEnumerable());
        }
        catch (Exception ex)
        {
            return Result.Error(new ErrorList(new[] { $"Error retrieving projects by tags: {ex.Message}" }, null));
        }
    }

    public async Task<Result<IEnumerable<Project>>> GetProjectsCreatedBetweenAsync(DateTime startDate, DateTime endDate)
    {
        try
        {
            if (startDate > endDate)
                return Result.Invalid(new[] { new ValidationError { ErrorMessage = "Start date cannot be after end date" } });

            var projects = await _dbSet
                .Where(p => p.CreatedAt >= startDate && p.CreatedAt <= endDate)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
            return Result.Success(projects.AsEnumerable());
        }
        catch (Exception ex)
        {
            return Result.Error(new ErrorList(new[] { $"Error retrieving projects created between dates: {ex.Message}" }, null));
        }
    }

    public async Task<Result<IEnumerable<Project>>> GetProjectsUpdatedSinceAsync(DateTime since)
    {
        try
        {
            var projects = await _dbSet
                .Where(p => p.UpdatedAt > since)
                .OrderByDescending(p => p.UpdatedAt)
                .ToListAsync();
            return Result.Success(projects.AsEnumerable());
        }
        catch (Exception ex)
        {
            return Result.Error(new ErrorList(new[] { $"Error retrieving projects updated since {since}: {ex.Message}" }, null));
        }
    }

    // File operations
    public async Task<Result<int>> GetFileCountForProjectAsync(Guid projectId)
    {
        try
        {
            var count = await _context.ProjectFiles
                .Where(f => f.ProjectId == projectId && f.IsActive)
                .CountAsync();
            return Result.Success(count);
        }
        catch (Exception ex)
        {
            return Result.Error(new ErrorList(new[] { $"Error counting files for project {projectId}: {ex.Message}" }, null));
        }
    }

    public async Task<Result<long>> GetTotalFileSizeForProjectAsync(Guid projectId)
    {
        try
        {
            var totalSize = await _context.ProjectFiles
                .Where(f => f.ProjectId == projectId && f.IsActive)
                .SumAsync(f => f.ContentSize);
            return Result.Success(totalSize);
        }
        catch (Exception ex)
        {
            return Result.Error(new ErrorList(new[] { $"Error calculating total file size for project {projectId}: {ex.Message}" }, null));
        }
    }

    public async Task<Result<IEnumerable<ProjectFile>>> GetGeneratedFilesAsync(Guid projectId)
    {
        try
        {
            var files = await _context.ProjectFiles
                .Where(f => f.ProjectId == projectId && f.IsGenerated && f.IsActive)
                .OrderBy(f => f.RelativePath)
                .ToListAsync();
            return Result.Success(files.AsEnumerable());
        }
        catch (Exception ex)
        {
            return Result.Error(new ErrorList(new[] { $"Error retrieving generated files for project {projectId}: {ex.Message}" }, null));
        }
    }

    public async Task<Result<IEnumerable<ProjectFile>>> GetFilesByTypeAsync(Guid projectId, string contentType)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(contentType))
                return Result<IEnumerable<ProjectFile>>.Invalid(new ValidationError { ErrorMessage = "Content type cannot be null or empty" });

            var files = await _context.ProjectFiles
                .Where(f => f.ProjectId == projectId && f.ContentType == contentType && f.IsActive)
                .OrderBy(f => f.RelativePath)
                .ToListAsync();
            return Result.Success(files.AsEnumerable());
        }
        catch (Exception ex)
        {
            return Result.Error(new ErrorList(new[] { $"Error retrieving files by type for project {projectId}: {ex.Message}" }, null));
        }
    }

    // Pipeline operations
    public async Task<Result<PipelineExecution>> GetLatestPipelineExecutionAsync(Guid projectId)
    {
        try
        {
            var execution = await _context.PipelineExecutions
                .Where(pe => pe.ProjectId == projectId)
                .OrderByDescending(pe => pe.StartedAt)
                .Include(pe => pe.StageExecutions)
                .Include(pe => pe.OrchestrationResults)
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

    public async Task<Result<IEnumerable<PipelineExecution>>> GetPipelineExecutionHistoryAsync(Guid projectId)
    {
        try
        {
            var executions = await _context.PipelineExecutions
                .Where(pe => pe.ProjectId == projectId)
                .OrderByDescending(pe => pe.StartedAt)
                .Include(pe => pe.StageExecutions)
                .ToListAsync();
            return Result.Success(executions.AsEnumerable());
        }
        catch (Exception ex)
        {
            return Result.Error(new ErrorList(new[] { $"Error retrieving pipeline execution history for project {projectId}: {ex.Message}" }, null));
        }
    }

    public async Task<Result<bool>> HasActivePipelineExecutionAsync(Guid projectId)
    {
        try
        {
            var hasActive = await _context.PipelineExecutions
                .AnyAsync(pe => pe.ProjectId == projectId && 
                               (pe.Status == "Running" || pe.Status == "InProgress"));
            return Result.Success(hasActive);
        }
        catch (Exception ex)
        {
            return Result.Error(new ErrorList(new[] { $"Error checking for active pipeline executions for project {projectId}: {ex.Message}" }, null));
        }
    }
}