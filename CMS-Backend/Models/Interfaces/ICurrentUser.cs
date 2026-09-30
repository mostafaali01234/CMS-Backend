using System.Security.Claims;

namespace CMS.Api.Models.Interfaces
{
    public interface ICurrentUser
    {
        string? UserId { get; }
    }

    public sealed class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser
    {
        public string? UserId =>
            accessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
    }

}
