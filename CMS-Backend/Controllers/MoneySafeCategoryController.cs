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
public class MoneySafeCategoryController : ApiControllerBase
{

    private readonly IMoneySafeCategoryService _catService;

    public MoneySafeCategoryController(IMoneySafeCategoryService catService)
    {
        _catService = catService;
    }

    // GET: api/MoneySafeCategory
    [HttpGet]
    public async Task<IActionResult> GetAll() =>
        ToActionResult(await _catService.GetAllAsync());

    // GET: api/MoneySafeCategory/5
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id) =>
        ToActionResult(await _catService.GetByIdAsync(id));

    // POST: api/MoneySafeCategory
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] MoneySafeCategory cat) =>
        ToActionResult(await _catService.CreateAsync(cat));

    // PUT: api/MoneySafeCategory/5
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] MoneySafeCategory cat) =>
        ToNoContentResult(await _catService.UpdateAsync(id, cat));

    // DELETE: api/MoneySafeCategory/5
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id) =>
        ToNoContentResult(await _catService.DeleteAsync(id));
}
