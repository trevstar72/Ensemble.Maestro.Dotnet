using FastEndpoints;
using Ensemble.Maestro.Dotnet.Core.Data.Repositories;
using Ardalis.Result;
using Ardalis.Result.AspNetCore;

namespace Ensemble.Maestro.Dotnet.Api.Projects;

/// <summary>
/// Endpoint to retrieve all projects
/// </summary>
public class GetProjectsEndpoint : EndpointWithoutRequest<GetProjectsResponse>
{
    private readonly IProjectRepository _projectRepository;

    public GetProjectsEndpoint(IProjectRepository projectRepository)
    {
        _projectRepository = projectRepository;
    }

    public override void Configure()
    {
        Get("/api/projects");
        AllowAnonymous(); // Will be secured later
        Description(b => b
            .WithName("Get Projects")
            .WithSummary("Retrieves all projects")
            .WithDescription("Returns a list of all projects in the system")
            .Produces<GetProjectsResponse>(200, "application/json"));
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var projectsResult = await _projectRepository.GetAllAsync();
        if (!projectsResult.IsSuccess)
        {
            await HandleResultErrorsAsync(projectsResult, ct);
            return;
        }

        var statisticsResult = await _projectRepository.GetProjectStatisticsAsync();
        if (!statisticsResult.IsSuccess)
        {
            await HandleResultErrorsAsync(statisticsResult, ct);
            return;
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

        await Send.OkAsync(response, ct);
    }

    private async Task HandleResultErrorsAsync<T>(Result<T> result, CancellationToken ct)
    {
        switch (result.Status)
        {
            case ResultStatus.NotFound:
                await Send.NotFoundAsync(ct);
                break;
            case ResultStatus.Invalid:
                ThrowError("Invalid request parameters", 400);
                break;
            case ResultStatus.Error:
            default:
                ThrowError("An error occurred while retrieving projects", 500);
                break;
        }
    }
}

/// <summary>
/// Response model for getting projects
/// </summary>
public class GetProjectsResponse
{
    public List<ProjectDto> Projects { get; set; } = new();
    public ProjectStatisticsDto Statistics { get; set; } = new();
}

/// <summary>
/// Project data transfer object
/// </summary>
public class ProjectDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Requirements { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? TargetLanguage { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Project statistics data transfer object
/// </summary>
public class ProjectStatisticsDto
{
    public int Total { get; set; }
    public int Completed { get; set; }
    public int InProgress { get; set; }
    public int Failed { get; set; }
}