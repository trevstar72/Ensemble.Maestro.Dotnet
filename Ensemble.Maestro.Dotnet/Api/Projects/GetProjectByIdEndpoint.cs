using FastEndpoints;
using Ensemble.Maestro.Dotnet.Core.Data.Repositories;
using Ardalis.Result;
using Ardalis.Result.AspNetCore;
using Microsoft.AspNetCore.Mvc;

namespace Ensemble.Maestro.Dotnet.Api.Projects;

/// <summary>
/// Endpoint to retrieve a project by ID using modern Ardalis.Result patterns
/// </summary>
public class GetProjectByIdEndpoint : Endpoint<GetProjectByIdRequest, ProjectDto>
{
    private readonly IProjectRepository _projectRepository;

    public GetProjectByIdEndpoint(IProjectRepository projectRepository)
    {
        _projectRepository = projectRepository;
    }

    public override void Configure()
    {
        Get("/api/projects/{id}");
        AllowAnonymous(); // Will be secured later
        Description(b => b
            .WithName("Get Project By ID")
            .WithSummary("Retrieves a project by its ID")
            .WithDescription("Returns a single project matching the specified ID")
            .Produces<ProjectDto>(200, "application/json")
            .Produces(404));
    }

    public override async Task HandleAsync(GetProjectByIdRequest req, CancellationToken ct)
    {
        var result = await _projectRepository.GetByIdAsync(req.Id);
        
        switch (result.Status)
        {
            case ResultStatus.Ok:
                var projectDto = new ProjectDto
                {
                    Id = result.Value.Id,
                    Name = result.Value.Name,
                    Requirements = result.Value.Requirements,
                    Status = result.Value.Status,
                    TargetLanguage = result.Value.TargetLanguage,
                    CreatedAt = result.Value.CreatedAt,
                    UpdatedAt = result.Value.UpdatedAt
                };
                await Send.OkAsync(projectDto, ct);
                break;
                
            case ResultStatus.NotFound:
                await Send.NotFoundAsync(ct);
                break;
                
            case ResultStatus.Invalid:
                ThrowError("Invalid request parameters", 400);
                break;
                
            case ResultStatus.Error:
            default:
                ThrowError("An error occurred while retrieving the project", 500);
                break;
        }
    }
}

/// <summary>
/// Request model for getting a project by ID
/// </summary>
public class GetProjectByIdRequest
{
    public Guid Id { get; set; }
}