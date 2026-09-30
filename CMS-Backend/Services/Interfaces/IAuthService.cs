using CMS.Api.Models.DTOs.Requests;
using CMS.Api.Models.DTOs.Responses;

namespace CMS.Api.Services.Interfaces;

public interface IAuthService
{
    Task<ServiceResult<AuthTokensDto>> RegisterAsync(UserRegisterationDto dto);
    Task<ServiceResult<AuthTokensDto>> LoginAsync(UserLoginDto dto);
    Task<ServiceResult<AuthTokensDto>> RefreshAsync(TokenRequest request);
}