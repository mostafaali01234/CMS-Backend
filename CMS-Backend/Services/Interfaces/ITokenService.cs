using CMS.Api.Models.DTOs.Responses;
using Microsoft.AspNetCore.Identity;

namespace CMS.Api.Services.Interfaces;

public interface ITokenService
{
    Task<AuthTokensDto> GenerateTokensAsync(IdentityUser user);
    Task<ServiceResult<AuthTokensDto>> RefreshTokensAsync(string accessToken, string refreshToken);
}