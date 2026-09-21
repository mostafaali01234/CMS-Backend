using CMS_Backend.Models.DTOs.Responses;

namespace CMS_Backend.Services.Interfaces;

public interface IUserRoleService
{
    Task<ServiceResult<List<RoleDto>>> GetRolesAsync(CancellationToken ct = default);
    Task<ServiceResult<RoleDto>> CreateRoleAsync(string name);
    Task<ServiceResult<List<UserDto>>> GetUsersAsync(CancellationToken ct = default);
    Task<ServiceResult<IList<string>>> GetUserRolesAsync(string email);
    Task<ServiceResult<string>> AddUserToRoleAsync(string email, string roleName);
    Task<ServiceResult<string>> RemoveUserFromRoleAsync(string email, string roleName);
}