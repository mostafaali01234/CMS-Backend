using CMS.Application.DTOs;
using CMS.Application.Interfaces;
using CMS.Domain.Enums;
using CMS_Backend.Controllers.Base;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

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
    //[HttpGet]
    //public async Task<IActionResult> GetAll() =>
    //    ToActionResult(await _orderService.GetAllAsync());
    [HttpGet]
    public async Task<IActionResult> GetAll(
    [FromQuery(Name = "page")] int page = 1,
    [FromQuery(Name = "page_size")] int pageSize = 20,
    [FromQuery(Name = "date_from")] DateTime? dateFrom = null,
    [FromQuery(Name = "date_to")] DateTime? dateTo = null,
    [FromQuery(Name = "search")] string? search = null,
    [FromQuery(Name = "items_category")] string? itemsCategory = null,
    [FromQuery(Name = "status")] OrderStatus? status = null,
    [FromQuery(Name = "type")] OrderType? type = null,
    [FromQuery(Name = "category_type")] OrderCategory? categoryType = null,
    [FromQuery(Name = "source")] OrderSource? source = null,
    [FromQuery(Name = "seller_id")] long? sellerId = null,
    [FromQuery(Name = "tech_id")] long? techId = null,
    [FromQuery(Name = "country_state_id")] long? countryStateId = null)
    {
        var dto = new OrderQueryDto
        {
            Page = page,
            PageSize = pageSize,
            DateFrom = dateFrom,
            DateTo = dateTo,
            Search = search,
            ItemsCategory = itemsCategory,
            Status = status,
            Type = type,
            Source = source,
            SellerId = sellerId,
            TechId = techId,
            CountryStateId = countryStateId,
            CategoryType = categoryType
        };
        return ToActionResult(await _orderService.GetPagedAsync(dto));
    }

    // GET: api/Order/5
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id) =>
        ToActionResult(await _orderService.GetByIdAsync(id));

    // POST: api/Order
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] OrderDto order) =>
        ToActionResult(await _orderService.CreateAsync(order));

    // POST: api/Order/Maintenance
    [HttpPost("Maintenance")]
    public async Task<IActionResult> CreateMaintenance([FromBody] OrderMaintenanceRequestDto request) =>
        ToActionResult(await _orderService.CreateMaintenanceAsync(request));


    // GET: api/Order/CustomerDone/5
    [HttpGet("CustomerDone/{id:int}")]
    public async Task<IActionResult> GetDoneByCustomerId(int id) =>
        ToActionResult(await _orderService.GetDoneByCustomerIdAsync(id));

    // GET: api/Order/Maintenance/5
    [HttpGet("Maintenance/{id:int}")]
    public async Task<IActionResult> GetOldMaintenance(int id) =>
        ToActionResult(await _orderService.GetOldMaintenanceAsync(id));

    // GET: api/Order/stats
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        var userId = User.FindFirstValue("UserId");       
        if (userId is null) return Unauthorized();

        return ToActionResult(await _orderService.GetStatsAsync(userId, User.IsInRole("SuperAdmin")));
    }


    // PUT: api/Order/5
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] OrderDto order) =>
        ToNoContentResult(await _orderService.UpdateAsync(id, order));

    // DELETE: api/Order/5
    [HttpDelete("{id:int}")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Delete(int id, [FromBody] OrderCancelDto body) =>
        ToNoContentResult(await _orderService.DeleteAsync(id, body));

    // DELETE: api/Order/5/cancel
    [HttpPut("{id:int}/cancel")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Cancel(int id, [FromBody] OrderCancelDto body) =>
        ToNoContentResult(await _orderService.CancelAsync(id, body));

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
