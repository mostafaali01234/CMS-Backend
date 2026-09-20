using CMS_Backend.Configuration;
using System.ComponentModel.DataAnnotations;

namespace CMS_Backend.Models.DTOs.Responses
{
    public class RegistrationResponse : AuthResult
    {

    }

    public record RoleDto(string Id, string? Name);

    // Deliberately excludes PasswordHash, SecurityStamp, ConcurrencyStamp, etc.
    public record UserDto(string Id, string? UserName, string? Email, bool EmailConfirmed);

    // Flat shape instead of serializing System.Security.Claims.Claim directly
    public record ClaimDto(string Type, string Value);
    
    // Replaces RegistrationResponse/AuthResult on the wire. Errors now travel as ProblemDetails,
    // so there is no Success/Errors here. JSON: { "token": "...", "refreshToken": "..." }
    public record AuthTokensDto(string Token, string RefreshToken);
}
