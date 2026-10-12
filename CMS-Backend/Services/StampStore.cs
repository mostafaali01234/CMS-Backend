// Services/BuyInvoiceService.cs
using CMS.Application.Interfaces.Configuration;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Distributed;

namespace CMS.Api.Services;

public class StampStore(
    IDistributedCache cache,
    UserManager<IdentityUser> users,
    ILogger<StampStore> logger) : IStampStore
{
    private static string Key(string userId) => $"stamp:{userId}";

    // TTL is only a safety net; explicit invalidation is what makes changes instant
    private static readonly DistributedCacheEntryOptions Options = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15)
    };

    public async Task<string?> GetAsync(string userId)
    {
        try
        {
            var cached = await cache.GetStringAsync(Key(userId));
            if (cached is not null) return cached;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Redis read failed, falling back to DB");
        }

        var user = await users.FindByIdAsync(userId);
        if (user is null) return null;                    // deleted user: token invalid

        var stamp = await users.GetSecurityStampAsync(user);
        if (stamp is null) return null;

        try { await cache.SetStringAsync(Key(userId), stamp, Options); }
        catch (Exception ex) { logger.LogWarning(ex, "Redis write failed"); }

        return stamp;
    }

    public Task InvalidateAsync(string userId) => cache.RemoveAsync(Key(userId));
}