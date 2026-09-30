using CMS.Api.Data;
using CMS.Application.DTOs;
using CMS.Application.Interfaces.Configuration;
using Microsoft.EntityFrameworkCore;

namespace CMS_Backend.Services.Configuration;

// Infrastructure
public class UserLookup : IUserLookup
{
    private readonly AppDbContext _db;
    public UserLookup(AppDbContext db) => _db = db;

    public Task<bool> ExistsAsync(string userId, CancellationToken ct = default)
        => _db.Users.AnyAsync(u => u.Id == userId, ct);
    public IQueryable<UserSummary> Query() =>
       _db.Users.AsNoTracking()
          .Select(u => new UserSummary(u.Id, u.Email, u.UserName));

    public async Task<TokenUser?> GetByIdAsync(string userId, CancellationToken ct = default)
    {
        var u = await _db.Users.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == userId, ct);
        return u is null ? null : new TokenUser(u.Id, u.UserName!, u.Email, Array.Empty<string>());
    }
}