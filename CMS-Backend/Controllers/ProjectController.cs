using CMS.Domain.Models;
using CMS.Application.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CMS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
//[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "SuperAdmin,Admin")]
public class ProjectController : ApiControllerBase
{

    private readonly IProjectService _projectService;

    public ProjectController(IProjectService projectService)
    {
        _projectService = projectService;
    }

    // GET: api/project
    [HttpGet]
    public async Task<IActionResult> GetAll() =>
        ToActionResult(await _projectService.GetAllAsync());

    // GET: api/project/5
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id) =>
        ToActionResult(await _projectService.GetByIdAsync(id));

    // POST: api/project
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Project project) =>
        ToActionResult(await _projectService.CreateAsync(project));

    // PUT: api/project/5
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] Project project) =>
        ToNoContentResult(await _projectService.UpdateAsync(id, project));

    // DELETE: api/project/5
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id) =>
        ToNoContentResult(await _projectService.DeleteAsync(id));
}
