using CMS_Backend.Configuration;
using CMS_Backend.Data;
using CMS_Backend.Models;
using CMS_Backend.Models.DTOs.Responses;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using CMS_Backend.Services.Interfaces;

namespace CMS_Backend.Services;

public class TokenService : ITokenService
{
    private const string InvalidTokens = "Invalid tokens";
    private static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromDays(365); // consider moving to JwtConfig

    private readonly UserManager<IdentityUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly AppDbContext _dbContext;
    private readonly JwtConfig _jwtConfig;
    private readonly TokenValidationParameters _tokenValidationParameters;
    private readonly ILogger<TokenService> _logger;

    public TokenService(
        UserManager<IdentityUser> userManager,
        RoleManager<IdentityRole> roleManager,
        AppDbContext dbContext,
        IOptionsMonitor<JwtConfig> jwtOptions,
        TokenValidationParameters tokenValidationParameters,
        ILogger<TokenService> logger)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _dbContext = dbContext;
        _jwtConfig = jwtOptions.CurrentValue;
        _tokenValidationParameters = tokenValidationParameters;
        _logger = logger;
    }

    public async Task<AuthTokensDto> GenerateTokensAsync(IdentityUser user)
    {
        var jwtHandler = new JwtSecurityTokenHandler();

        // Must stay in sync with how the key is built in the TokenValidationParameters (Program.cs)
        var key = Encoding.ASCII.GetBytes(_jwtConfig.Secret);

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(await BuildClaimsAsync(user)),
            Expires = DateTime.UtcNow.Add(_jwtConfig.ExpiryTimeFrame),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = jwtHandler.CreateToken(descriptor);

        var refreshToken = new RefreshToken
        {
            JwtId = token.Id,
            IsUsed = false,
            IsRevoked = false,
            UserId = user.Id,
            AddedDate = DateTime.UtcNow,
            ExpiryDate = DateTime.UtcNow.Add(RefreshTokenLifetime),
            Token = GenerateRefreshTokenValue()
        };

        _dbContext.RefreshTokens.Add(refreshToken);
        await _dbContext.SaveChangesAsync();

        return new AuthTokensDto(jwtHandler.WriteToken(token), refreshToken.Token);
    }

    public async Task<ServiceResult<AuthTokensDto>> RefreshTokensAsync(string accessToken, string refreshToken)
    {
        // The specific reason is logged; the client only ever sees a generic message.
        ServiceResult<AuthTokensDto> Reject(string reason)
        {
            _logger.LogWarning("Refresh rejected: {Reason}", reason);
            return ServiceResult<AuthTokensDto>.Unauthorized(InvalidTokens);
        }

        var jwt = ValidateAccessTokenIgnoringLifetime(accessToken);
        if (jwt is null)
            return Reject("access token failed validation");

        // Kept from the original behaviour: refreshing is only allowed once the access token has expired.
        // Delete this check if clients should be able to refresh proactively.
        if (jwt.ValidTo > DateTime.UtcNow)
            return ServiceResult<AuthTokensDto>.Failed("The access token has not expired yet");

        var stored = await _dbContext.RefreshTokens.FirstOrDefaultAsync(x => x.Token == refreshToken);

        if (stored is null) return Reject("refresh token does not exist");
        if (stored.ExpiryDate < DateTime.UtcNow) return Reject("refresh token has expired");
        if (stored.IsUsed) return Reject("refresh token has already been used");
        if (stored.IsRevoked) return Reject("refresh token has been revoked");
        if (stored.JwtId != jwt.Id) return Reject("refresh token does not match the access token");

        stored.IsUsed = true;
        await _dbContext.SaveChangesAsync();

        var user = await _userManager.FindByIdAsync(stored.UserId);
        if (user is null) return Reject("user no longer exists");

        return ServiceResult<AuthTokensDto>.Ok(await GenerateTokensAsync(user));
    }

    private JwtSecurityToken? ValidateAccessTokenIgnoringLifetime(string accessToken)
    {
        // Clone so the shared singleton is never mutated: the JWT bearer middleware keeps enforcing
        // lifetime, and only this path skips it. Signature, issuer, audience etc. are still checked.
        var parameters = _tokenValidationParameters.Clone();
        parameters.ValidateLifetime = false;

        try
        {
            new JwtSecurityTokenHandler().ValidateToken(accessToken, parameters, out var validated);

            return validated is JwtSecurityToken jwt &&
                   jwt.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase)
                ? jwt
                : null;
        }
        catch (Exception ex) when (ex is SecurityTokenException or ArgumentException)
        {
            _logger.LogWarning(ex, "Access token failed validation during refresh");
            return null;
        }
    }

    private async Task<List<Claim>> BuildClaimsAsync(IdentityUser user)
    {
        var claims = new List<Claim>
        {
            new Claim("UserId", user.Id),
            new Claim(JwtRegisteredClaimNames.Email, user.Email!),
            new Claim(JwtRegisteredClaimNames.Sub, user.Email!),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        claims.AddRange(await _userManager.GetClaimsAsync(user));

        foreach (var roleName in await _userManager.GetRolesAsync(user))
        {
            var role = await _roleManager.FindByNameAsync(roleName);
            if (role is null) continue;

            claims.Add(new Claim(ClaimTypes.Role, roleName));
            claims.AddRange(await _roleManager.GetClaimsAsync(role));
        }

        return claims;
    }

    // Cryptographically secure (System.Random is not), 256 bits, URL-safe
    private static string GenerateRefreshTokenValue() =>
        WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
}