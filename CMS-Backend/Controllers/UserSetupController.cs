using CMS_Backend.Models.DTOs.Requests;
using CMS_Backend.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CMS_Backend.Controllers
{
    [Route("api/[controller]")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "SuperAdmin,Admin")]
    public class UserSetupController : ApiControllerBase
    {
        private readonly IUserRoleService _userRoleService;

        public UserSetupController(IUserRoleService userRoleService)
        {
            _userRoleService = userRoleService;
        }

        [HttpGet("Roles")]
        public async Task<IActionResult> GetAllRoles(CancellationToken ct) =>
            ToActionResult(await _userRoleService.GetRolesAsync(ct));

        [HttpPost("Roles")]
        public async Task<IActionResult> CreateRole([FromBody] CreateRoleRequest request) =>
            ToActionResult(await _userRoleService.CreateRoleAsync(request.Name));

        [HttpGet("Users")]
        public async Task<IActionResult> GetAllUsers(CancellationToken ct) =>
            ToActionResult(await _userRoleService.GetUsersAsync(ct));

        [HttpGet("UserRole")]
        public async Task<IActionResult> GetUserRoles([FromQuery] string email) =>
            ToActionResult(await _userRoleService.GetUserRolesAsync(email));

        [HttpPost("UserRole")]
        public async Task<IActionResult> AddUserToRole([FromBody] UserRoleRequest request) =>
            ToNoContentResult(await _userRoleService.AddUserToRoleAsync(request.Email, request.RoleName));

        [HttpPost("UserRole/remove")]
        public async Task<IActionResult> RemoveUserFromRole([FromBody] UserRoleRequest request) =>
            ToNoContentResult(await _userRoleService.RemoveUserFromRoleAsync(request.Email, request.RoleName));
    }
}