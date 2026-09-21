using CMS_Backend.Models.DTOs.Requests;
using CMS_Backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CMS_Backend.Controllers;

[Route("api/[controller]")]
[AllowAnonymous]
public class AuthManagementController : ApiControllerBase
{
    private readonly IAuthService _authService;

    public AuthManagementController(IAuthService authService)
    {
        _authService = authService;
    }

    // [ApiController] already returns a 400 ValidationProblemDetails for an invalid model,
    // so the `if (ModelState.IsValid)` wrappers are gone.

    [HttpPost("Register")]
    public async Task<IActionResult> Register([FromBody] UserRegisterationDto dto) =>
        ToActionResult(await _authService.RegisterAsync(dto));

    [HttpPost("Login")]
    public async Task<IActionResult> Login([FromBody] UserLoginDto dto) =>
        ToActionResult(await _authService.LoginAsync(dto));

    [HttpPost("RefreshToken")]
    public async Task<IActionResult> RefreshToken([FromBody] TokenRequest request) =>
        ToActionResult(await _authService.RefreshAsync(request));
}
