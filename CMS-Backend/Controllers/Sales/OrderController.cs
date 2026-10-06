using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CMS_Backend.Controllers.Base;
using CMS.Application.Interfaces;
using CMS.Application.DTOs;

namespace CMS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
//[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "SuperAdmin,Admin,Sales,Tech")]
public class OrderController : ApiControllerBase
{

    private readonly IOrderService _orderService;
    private readonly IOrderTechHistoryService _techService;
    private readonly IOrderNoteHistoryService _noteService;

    public OrderController(IOrderService orderService
                            , IOrderTechHistoryService techService
                            , IOrderNoteHistoryService noteService)
    {
        _orderService = orderService;
        _techService = techService;
        _noteService = noteService;
    }

    // GET: api/Order
    [HttpGet]
    public async Task<IActionResult> GetAll() =>
        ToActionResult(await _orderService.GetAllAsync());

    // GET: api/Order/5
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id) =>
        ToActionResult(await _orderService.GetByIdAsync(id));

    // POST: api/Order
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] OrderDto order) =>
        ToActionResult(await _orderService.CreateAsync(order));

    // PUT: api/Order/5
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] OrderDto order) =>
        ToNoContentResult(await _orderService.UpdateAsync(id, order));

    // DELETE: api/Order/5
    [HttpDelete("{id:int}")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Delete(int id) =>
        ToNoContentResult(await _orderService.DeleteAsync(id));

    // DELETE: api/Order/5/cancel
    [HttpPut("{id:int}/cancel")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Cancel(int id) =>
        ToNoContentResult(await _orderService.CancelAsync(id));

    //----------------------------------------------------------------- Tech Endpoints -----------------------------------------------------------------

    // POST: api/Order/5/Tech
    [HttpPost("{id:int}/Tech")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> ChangeOrderTech([FromBody] OrderTechHistoryDto tech) =>
        ToActionResult(await _techService.CreateAsync(tech));

    //----------------------------------------------------------------- Notes Endpoints -----------------------------------------------------------------

    // GET: api/Order/5/Notes
    [HttpGet("{id:int}/Notes")]
    public async Task<IActionResult> GetNotesByOrderId(int id) =>
        ToActionResult(await _noteService.GetByOrderIdAsync(id));


    // POST: api/Order/5/Notes
    [HttpPost("{id:int}/Notes")]
    public async Task<IActionResult> AddOrderNotes([FromBody] OrderNoteHistoryDto notes) =>
        ToActionResult(await _noteService.CreateAsync(notes));

}
