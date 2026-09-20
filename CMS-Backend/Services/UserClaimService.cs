using CMS_Backend.Models.DTOs.Responses;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace CMS_Backend.Services
{
    public interface IUserClaimService
    {
        Task<ServiceResult<List<ClaimDto>>> GetUserClaimsAsync(string email);
        Task<ServiceResult<string>> AddClaimToUserAsync(string email, string claimType, string claimValue);
    }

    public class UserClaimService : IUserClaimService
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<UserClaimService> _logger;

        public UserClaimService(UserManager<IdentityUser> userManager, ILogger<UserClaimService> logger)
        {
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<ServiceResult<List<ClaimDto>>> GetUserClaimsAsync(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user is null)
                return ServiceResult<List<ClaimDto>>.NotFound("User doesn't exist");

            var claims = await _userManager.GetClaimsAsync(user);
            return ServiceResult<List<ClaimDto>>.Ok(
                claims.Select(c => new ClaimDto(c.Type, c.Value)).ToList());
        }

        public async Task<ServiceResult<string>> AddClaimToUserAsync(string email, string claimType, string claimValue)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user is null)
                return ServiceResult<string>.NotFound("User doesn't exist");

            // Identity doesn't enforce uniqueness on (user, type, value), so AddClaimAsync
            // would happily insert duplicate rows. Check explicitly.
            var existing = await _userManager.GetClaimsAsync(user);
            if (existing.Any(c => c.Type == claimType && c.Value == claimValue))
                return ServiceResult<string>.Conflict("User already has this claim");

            var result = await _userManager.AddClaimAsync(user, new Claim(claimType, claimValue));
            if (!result.Succeeded)
                return ServiceResult<string>.Failed(result.Errors.Select(e => e.Description));

            _logger.LogInformation("Claim {ClaimType} added to user {UserId}", claimType, user.Id);
            return ServiceResult<string>.Ok("Claim has been added to the user");
        }
    }
}