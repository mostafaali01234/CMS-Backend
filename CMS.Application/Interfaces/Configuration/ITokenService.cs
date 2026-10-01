using CMS.Application.DTOs;
using CMS.Application.DTOs.Responses;
//using Microsoft.AspNetCore.Identity;

namespace CMS.Application.Interfaces;

public interface ITokenService
{
    Task<AuthTokensDto> GenerateTokensAsync(string userId);
    //Task<AuthTokensDto> GenerateTokensAsync(TokenUser user);
    //Task<AuthTokensDto> GenerateTokensAsync(IdentityUser user);
    Task<ServiceResult<AuthTokensDto>> RefreshTokensAsync(string accessToken, string refreshToken);
}