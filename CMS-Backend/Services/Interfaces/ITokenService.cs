using CMS_Backend.Models.DTOs.Responses;
using Microsoft.AspNetCore.Identity;

namespace CMS_Backend.Services.Interfaces;

public interface ITokenService
{
    Task<AuthTokensDto> GenerateTokensAsync(IdentityUser user);
    Task<ServiceResult<AuthTokensDto>> RefreshTokensAsync(string accessToken, string refreshToken);
}