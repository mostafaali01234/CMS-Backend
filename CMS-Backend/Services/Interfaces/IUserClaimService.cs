using CMS.Api.Models.DTOs.Responses;

namespace CMS.Api.Services.Interfaces;

public interface IUserClaimService
{
    Task<ServiceResult<List<ClaimDto>>> GetUserClaimsAsync(string email);
    Task<ServiceResult<string>> AddClaimToUserAsync(string email, string claimType, string claimValue);
}