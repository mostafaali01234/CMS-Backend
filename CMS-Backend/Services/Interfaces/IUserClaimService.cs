using CMS_Backend.Models.DTOs.Responses;

namespace CMS_Backend.Services.Interfaces;

public interface IUserClaimService
{
    Task<ServiceResult<List<ClaimDto>>> GetUserClaimsAsync(string email);
    Task<ServiceResult<string>> AddClaimToUserAsync(string email, string claimType, string claimValue);
}