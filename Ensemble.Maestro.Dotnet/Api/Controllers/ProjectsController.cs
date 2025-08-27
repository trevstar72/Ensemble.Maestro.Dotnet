using Microsoft.AspNetCore.Mvc;
using Ardalis.Result;
using Ardalis.Result.AspNetCore;
using Ensemble.Maestro.Dotnet.Core.Services;
using Ensemble.Maestro.Dotnet.Api.Projects;

namespace Ensemble.Maestro.Dotnet.Api.Controllers;

/// <summary>
/// Modern controller demonstrating Ardalis.Result v10+ patterns
/// </summary>
[ApiController]
[Route("api/v2/[controller]")]
[Produces("application/json")]
public class ProjectsController : ControllerBase
{
    private readonly IProjectService _projectService;

    public ProjectsController(IProjectService projectService)
    {
        _projectService = projectService;
    }

    /// <summary>
    /// Gets all projects using automatic Result to ActionResult translation
    /// </summary>
    /// <returns>List of all projects with statistics</returns>
    [HttpGet]
    [TranslateResultToActionResult]
    [ExpectedFailures(ResultStatus.Error)]
    [ProducesResponseType(typeof(GetProjectsResponse), 200)]
    [ProducesResponseType(500)]
    public async Task<Result<GetProjectsResponse>> GetAllProjects()
    {
        return await _projectService.GetAllProjectsAsync();
    }

    /// <summary>
    /// Gets a project by ID using automatic Result to ActionResult translation
    /// </summary>
    /// <param name="id">The project ID</param>
    /// <returns>The project if found</returns>
    [HttpGet("{id:guid}")]
    [TranslateResultToActionResult]
    [ExpectedFailures(ResultStatus.NotFound, ResultStatus.Invalid)]
    [ProducesResponseType(typeof(ProjectDto), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(400)]
    public async Task<Result<ProjectDto>> GetProjectById(Guid id)
    {
        return await _projectService.GetProjectByIdAsync(id);
    }

    /// <summary>
    /// Alternative method using extension method for Result translation
    /// </summary>
    /// <param name="id">The project ID</param>
    /// <returns>ActionResult with project data</returns>
    [HttpGet("alternative/{id:guid}")]
    [ProducesResponseType(typeof(ProjectDto), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(400)]
    public async Task<ActionResult<ProjectDto>> GetProjectByIdAlternative(Guid id)
    {
        var result = await _projectService.GetProjectByIdAsync(id);
        return result.ToActionResult(this);
    }
}