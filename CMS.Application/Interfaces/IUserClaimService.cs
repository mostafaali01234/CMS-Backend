using CMS.Application.DTOs.Responses;

namespace CMS.Application.Interfaces;

public interface IUserClaimService
{
    Task<ServiceResult<List<ClaimDto>>> GetUserClaimsAsync(string email);
    Task<ServiceResult<string>> AddClaimToUserAsync(string email, string claimType, string claimValue);
}