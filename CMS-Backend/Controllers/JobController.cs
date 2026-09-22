using CMS_Backend.Models;
using CMS_Backend.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CMS_Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
//[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "SuperAdmin,Admin")]
public class JobController : ApiControllerBase
{

    private readonly IJobService _jobService;

    public JobController(IJobService jobService)
    {
        _jobService = jobService;
    }

    // GET: api/jobs
    [HttpGet]
    public async Task<IActionResult> GetAll() =>
        ToActionResult(await _jobService.GetAllAsync());

    // GET: api/jobs/5
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id) =>
        ToActionResult(await _jobService.GetByIdAsync(id));

    // POST: api/jobs
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Job job) =>
        ToActionResult(await _jobService.CreateAsync(job));

    // PUT: api/jobs/5
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] Job job) =>
        ToNoContentResult(await _jobService.UpdateAsync(id, job));

    // DELETE: api/jobs/5
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id) =>
        ToNoContentResult(await _jobService.DeleteAsync(id));
}
