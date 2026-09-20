using CMS_Backend.Models.DTOs.Requests;
using CMS_Backend.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CMS_Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "SuperAdmin,Admin")]
    public class ClaimsSetupController : ApiControllerBase
    {
        private readonly IUserClaimService _userClaimService;

        public ClaimsSetupController(IUserClaimService userClaimService)
        {
            _userClaimService = userClaimService;
        }

        [HttpGet]
        public async Task<IActionResult> GetUserClaims([FromQuery] string email) =>
            ToActionResult(await _userClaimService.GetUserClaimsAsync(email));

        [HttpPost("UserClaim")]
        public async Task<IActionResult> AddClaimToUser([FromBody] AddUserClaimRequest request) =>
            ToNoContentResult(await _userClaimService.AddClaimToUserAsync(
                request.Email, request.ClaimType, request.ClaimValue));
    }
}