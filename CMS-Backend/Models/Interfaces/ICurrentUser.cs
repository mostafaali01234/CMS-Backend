using System.Security.Claims;

namespace CMS_Backend.Models.Interfaces
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
