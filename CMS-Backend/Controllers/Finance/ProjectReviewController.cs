using CMS.Application.DTOs;
using CMS.Application.Interfaces;
using CMS_Backend.Controllers.Base;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CMS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[ApiVersion("1.0")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "SuperAdmin,Admin")]
public class ProjectReviewController : ApiControllerBase
{
    private readonly IProjectReviewService _service;
    public ProjectReviewController(IProjectReviewService service) { _service = service; }

    // GET: api/ProjectReview?dateFrom=2026-01-01&dateTo=2026-12-31   -> one summary row per project
    [HttpGet]
    public async Task<IActionResult> GetSummaries([FromQuery] ProjectReviewFilterDto f) =>
        ToActionResult(await _service.GetSummariesAsync(f));

    // GET: api/ProjectReview/5?dateFrom=&dateTo=   -> summary + every record tied to the project
    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetReview(long id, [FromQuery] ProjectReviewFilterDto f) =>
        ToActionResult(await _service.GetReviewAsync(id, f));

    // GET: api/ProjectReview/5/export?dateFrom=&dateTo=   -> .xlsx (summary + one sheet per record type)
    [HttpGet("{id:long}/export")]
    public async Task<IActionResult> Export(long id, [FromQuery] ProjectReviewFilterDto f)
    {
        var r = await _service.ExportAsync(id, f);
        return r.Error is not null
            ? BadRequest(new { message = r.Error })
            : File(r.Bytes!, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", r.FileName);
    }
}
