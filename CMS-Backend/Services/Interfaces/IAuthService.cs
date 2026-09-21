using CMS_Backend.Models.DTOs.Requests;
using CMS_Backend.Models.DTOs.Responses;

namespace CMS_Backend.Services.Interfaces;

public interface IAuthService
{
    Task<ServiceResult<AuthTokensDto>> RegisterAsync(UserRegisterationDto dto);
    Task<ServiceResult<AuthTokensDto>> LoginAsync(UserLoginDto dto);
    Task<ServiceResult<AuthTokensDto>> RefreshAsync(TokenRequest request);
}