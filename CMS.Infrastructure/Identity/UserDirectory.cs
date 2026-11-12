using CMS.Application.Interfaces.Configuration;
using CMS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CMS.Infrastructure.Identity;

public class UserDirectory : IUserDirectory
{
    private readonly AppDbContext _db;
    public UserDirectory(AppDbContext db) { _db = db; }

    public Task<List<string>> GetUserIdsInRolesAsync(params string[] roles) =>
        (from ur in _db.UserRoles
         join r in _db.Roles on ur.RoleId equals r.Id
         where roles.Contains(r.Name!)
         select ur.UserId).Distinct().ToListAsync();

    public Task<bool> UserExistsAsync(string userId) => _db.Users.AnyAsync(u => u.Id == userId);
}
