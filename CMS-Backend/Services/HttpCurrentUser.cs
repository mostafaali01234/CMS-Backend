using CMS.Application.Interfaces.Configuration;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace CMS.Api.Services;
public class HttpCurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _accessor;
    public HttpCurrentUser(IHttpContextAccessor accessor) => _accessor = accessor;
    private ClaimsPrincipal? Principal => _accessor.HttpContext?.User;

    public long? CurrentLogId =>
    _accessor.HttpContext?.Items["CurrentLogId"] as long?;
    public string? UserId =>
        Principal?.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? Principal?.FindFirstValue(JwtRegisteredClaimNames.Sub);

    public string? UserName => Principal?.Identity?.Name;

    public string? CorrelationId =>
        _accessor.HttpContext?.Items["CorrelationId"]?.ToString();
}