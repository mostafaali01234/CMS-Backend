using CMS.Application.Interfaces.Configuration;
using System.Security.Claims;

namespace CMS.Api.Models.Interfaces
{
    public sealed class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser
    {
        public string? UserId =>
            accessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
    }

}
