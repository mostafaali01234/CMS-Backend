using CMS.Application.DTOs;
using CMS.Application.Interfaces.Configuration;
using CMS.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CMS.Api.Services;

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

    public async Task<Dictionary<string, string>> GetEmailsByIdsAsync(IReadOnlyCollection<string> ids)
    {
        return await _db.Users              // or _identityDb.Users
            .Where(u => ids.Contains(u.Id))          // filter on the entity, before any projection
            .Select(u => new { u.Id, u.Email })
            .ToDictionaryAsync(u => u.Id, u => u.Email ?? "");
    }

    public async Task<List<UserListDto>> GetUsersAsync()
    {
        return await _db.Users
            .Select(u => new UserListDto((_db.Employee.FirstOrDefault(x => x.UserId == u.Id).Name ?? ""), u.Email, u.Id))
            .ToListAsync();
    }
}