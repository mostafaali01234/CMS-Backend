using CMS.Application.DTOs;
using CMS.Application.DTOs.Requests;
using CMS.Application.Interfaces;
using CMS_Backend.Controllers.Base;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CMS.Api.Controllers;

[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class UserSetupController : ApiControllerBase
{
    private readonly IUserRoleService _userRoleService;
    private readonly ITokenService _tokenService;
    private readonly UserManager<IdentityUser> _userManager;

    public UserSetupController(IUserRoleService userRoleService, ITokenService tokenService, UserManager<IdentityUser> userManager)
    {
        _userRoleService = userRoleService;
        _userManager = userManager;
        _tokenService = tokenService;
    }

    #region Profile
    private async Task<IdentityUser?> CurrentUser()
    {
        var id = User.FindFirstValue("UserId") ?? User.FindFirstValue("sub");
        return id is null ? null : await _userManager.FindByIdAsync(id);
    }

    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var user = await CurrentUser();
        if (user is null) return Unauthorized();

        return Ok(new ProfileDto(user.UserName, user.Email!, user.PhoneNumber));
    }

    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile(UpdateProfileDto dto)
    {
        var user = await CurrentUser();
        if (user is null) return Unauthorized();

        user.UserName = dto.FullName.Trim();
        user.PhoneNumber = string.IsNullOrWhiteSpace(dto.PhoneNumber) ? null : dto.PhoneNumber.Trim();

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });

        return Ok(new ProfileDto(user.UserName, user.Email!, user.PhoneNumber));
    }

    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword(ChangePasswordDto dto)
    {
        var user = await CurrentUser();
        if (user is null) return Unauthorized();

        var result = await _userManager.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword);
        if (!result.Succeeded)
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });


        await _userRoleService.ChangeSecurityStamp(user.Id, true);
        await _tokenService.RevokeAllAsync(user.Id);

        return NoContent();
    }

    [HttpPost("change-password/admin")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "SuperAdmin")]
    public async Task<IActionResult> ResetPasswordAdmin(ResetPasswordAdminDto dto)
    {
        var user = await _userManager.FindByIdAsync(dto.UserId);
        if (user is null) return NotFound();
        //var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        //var result = await _userManager.ResetPasswordAsync(user, token, "P@ssw0rd");
        var removeResult = await _userManager.RemovePasswordAsync(user);
        if (removeResult.Succeeded)
        {
            // Add new password
            var result = await _userManager.AddPasswordAsync(user, "P@ssw0rd");

            if (!result.Succeeded)
                return BadRequest(new { errors = result.Errors.Select(e => e.Description) });


            await _userRoleService.ChangeSecurityStamp(user.Id, true);
            await _tokenService.RevokeAllAsync(user.Id);
        }

        return NoContent();
    }
    #endregion

    #region Roles

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

    #endregion
}