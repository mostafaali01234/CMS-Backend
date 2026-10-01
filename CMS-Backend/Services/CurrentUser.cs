using CMS.Application.Interfaces.Configuration;
using System.Security.Claims;

namespace CMS.Api.Services
{
    public sealed class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser
    {
        public string? UserId =>
            accessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);

        public string? UserName => throw new NotImplementedException();

        public string? CorrelationId => throw new NotImplementedException();

        public long? CurrentLogId => throw new NotImplementedException();
    }

}
