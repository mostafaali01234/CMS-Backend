using CMS.Application.DTOs;

namespace CMS.Application.Interfaces.Configuration;
public interface IUserLookup
{
    IQueryable<UserSummary> Query();
    Task<bool> ExistsAsync(string userId, CancellationToken ct = default);
    Task<TokenUser?> GetByIdAsync(string userId, CancellationToken ct = default);
    Task<Dictionary<string, string>> GetEmailsByIdsAsync(IReadOnlyCollection<string> ids);
    Task<List<UserListDto>> GetUsersAsync();
}