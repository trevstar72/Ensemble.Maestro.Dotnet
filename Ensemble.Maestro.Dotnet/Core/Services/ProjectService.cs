using Ardalis.Result;
using Ensemble.Maestro.Dotnet.Core.Data.Repositories;
using Ensemble.Maestro.Dotnet.Api.Projects;

namespace Ensemble.Maestro.Dotnet.Core.Services;

/// <summary>
/// Service layer for Project operations using modern Result patterns
/// </summary>
public class ProjectService : IProjectService
{
    private readonly IProjectRepository _projectRepository;

    public ProjectService(IProjectRepository projectRepository)
    {
        _projectRepository = projectRepository;
    }

    public async Task<Result<ProjectDto>> GetProjectByIdAsync(Guid id)
    {
        var projectResult = await _projectRepository.GetByIdAsync(id);
        
        return projectResult.Status switch
        {
            ResultStatus.Ok => projectResult.Map(project => new ProjectDto
            {
                Id = project.Id,
                Name = project.Name,
                Requirements = project.Requirements,
                Status = project.Status,
                TargetLanguage = project.TargetLanguage,
                CreatedAt = project.CreatedAt,
                UpdatedAt = project.UpdatedAt
            }),
            ResultStatus.NotFound => Result<ProjectDto>.NotFound($"Project with ID {id} not found"),
            ResultStatus.Invalid => Result<ProjectDto>.Invalid(projectResult.ValidationErrors),
            _ => Result<ProjectDto>.Error(new ErrorList(projectResult.Errors, null))
        };
    }

    public async Task<Result<GetProjectsResponse>> GetAllProjectsAsync()
    {
        var projectsResult = await _projectRepository.GetAllAsync();
        if (!projectsResult.IsSuccess)
        {
            return Result<GetProjectsResponse>.Error(new ErrorList(projectsResult.Errors, null));
        }

        var statisticsResult = await _projectRepository.GetProjectStatisticsAsync();
        if (!statisticsResult.IsSuccess)
        {
            return Result<GetProjectsResponse>.Error(new ErrorList(statisticsResult.Errors, null));
        }

        var response = new GetProjectsResponse
        {
            Projects = projectsResult.Value.Select(p => new ProjectDto
            {
                Id = p.Id,
                Name = p.Name,
                Requirements = p.Requirements,
                Status = p.Status,
                TargetLanguage = p.TargetLanguage,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt
            }).ToList(),
            Statistics = new ProjectStatisticsDto
            {
                Total = statisticsResult.Value.Total,
                Completed = statisticsResult.Value.Completed,
                InProgress = statisticsResult.Value.InProgress,
                Failed = statisticsResult.Value.Failed
            }
        };

        return Result.Success(response);
    }

    public async Task<List<Data.Entities.Project>> GetProjectEntitiesAsync()
    {
        var result = await _projectRepository.GetAllAsync();
        return result.IsSuccess ? result.Value.ToList() : new List<Data.Entities.Project>();
    }
}

/// <summary>
/// Interface for Project service operations
/// </summary>
public interface IProjectService
{
    Task<Result<ProjectDto>> GetProjectByIdAsync(Guid id);
    Task<Result<GetProjectsResponse>> GetAllProjectsAsync();
    Task<List<Data.Entities.Project>> GetProjectEntitiesAsync();
}