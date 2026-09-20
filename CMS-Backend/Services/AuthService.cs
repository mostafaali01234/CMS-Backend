using CMS_Backend.Models.DTOs.Requests;
using CMS_Backend.Models.DTOs.Responses;
using Microsoft.AspNetCore.Identity;

namespace CMS_Backend.Services
{
    public interface IAuthService
    {
        Task<ServiceResult<AuthTokensDto>> RegisterAsync(UserRegisterationDto dto);
        Task<ServiceResult<AuthTokensDto>> LoginAsync(UserLoginDto dto);
        Task<ServiceResult<AuthTokensDto>> RefreshAsync(TokenRequest request);
    }

    public class AuthService : IAuthService
    {
        private const string DefaultRole = "Sales";
        private const string InvalidCredentials = "Invalid email or password";

        private readonly UserManager<IdentityUser> _userManager;
        private readonly ITokenService _tokenService;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            UserManager<IdentityUser> userManager,
            ITokenService tokenService,
            ILogger<AuthService> logger)
        {
            _userManager = userManager;
            _tokenService = tokenService;
            _logger = logger;
        }

        public async Task<ServiceResult<AuthTokensDto>> RegisterAsync(UserRegisterationDto dto)
        {
            if (await _userManager.FindByEmailAsync(dto.Email) is not null)
                return ServiceResult<AuthTokensDto>.Conflict("Email already in use");

            var user = new IdentityUser { Email = dto.Email, UserName = dto.UserName };

            var created = await _userManager.CreateAsync(user, dto.Password);
            if (!created.Succeeded)
                return ServiceResult<AuthTokensDto>.Failed(created.Errors.Select(e => e.Description));

            // The original ignored this result, so a missing "Sales" role silently left a role-less account.
            var roleResult = await _userManager.AddToRoleAsync(user, DefaultRole);
            if (!roleResult.Succeeded)
            {
                _logger.LogError("Could not assign default role {Role} to new user {UserId}: {Errors}",
                    DefaultRole, user.Id, string.Join("; ", roleResult.Errors.Select(e => e.Description)));

                await _userManager.DeleteAsync(user); // don't leave a half-registered account behind
                return ServiceResult<AuthTokensDto>.Failed("Registration failed. Please try again later.");
            }

            return ServiceResult<AuthTokensDto>.Ok(await _tokenService.GenerateTokensAsync(user));
        }

        public async Task<ServiceResult<AuthTokensDto>> LoginAsync(UserLoginDto dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);

            // Same message for "no such email" and "wrong password" so the endpoint can't be used
            // to find out which emails are registered.
            if (user is null)
                return ServiceResult<AuthTokensDto>.Unauthorized(InvalidCredentials);

            if (await _userManager.IsLockedOutAsync(user))
                return ServiceResult<AuthTokensDto>.Unauthorized("Account is temporarily locked. Try again later.");

            if (!await _userManager.CheckPasswordAsync(user, dto.Password))
            {
                await _userManager.AccessFailedAsync(user); // counts toward lockout
                return ServiceResult<AuthTokensDto>.Unauthorized(InvalidCredentials);
            }

            await _userManager.ResetAccessFailedCountAsync(user);
            return ServiceResult<AuthTokensDto>.Ok(await _tokenService.GenerateTokensAsync(user));
        }

        public Task<ServiceResult<AuthTokensDto>> RefreshAsync(TokenRequest request) =>
            _tokenService.RefreshTokensAsync(request.Token, request.RefreshToken);
    }
}