using CMS.Application.DTOs.Requests;
using CMS.Application.DTOs.Responses;

namespace CMS.Application.Interfaces.Configuration;

public interface IAuthService
{
    Task<ServiceResult<AuthTokensDto>> RegisterAsync(UserRegisterationDto dto);
    Task<ServiceResult<AuthTokensDto>> LoginAsync(UserLoginDto dto);
    Task<ServiceResult<AuthTokensDto>> RefreshAsync(TokenRequest request);
}