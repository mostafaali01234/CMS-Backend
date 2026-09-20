using CMS_Backend.Models.DTOs.Responses;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CMS_Backend.Services
{
    public interface IUserRoleService
    {
        Task<ServiceResult<List<RoleDto>>> GetRolesAsync(CancellationToken ct = default);
        Task<ServiceResult<RoleDto>> CreateRoleAsync(string name);
        Task<ServiceResult<List<UserDto>>> GetUsersAsync(CancellationToken ct = default);
        Task<ServiceResult<IList<string>>> GetUserRolesAsync(string email);
        Task<ServiceResult<string>> AddUserToRoleAsync(string email, string roleName);
        Task<ServiceResult<string>> RemoveUserFromRoleAsync(string email, string roleName);
    }

    public class UserRoleService : IUserRoleService
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ILogger<UserRoleService> _logger;

        public UserRoleService(
            UserManager<IdentityUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ILogger<UserRoleService> logger)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _logger = logger;
        }

        public async Task<ServiceResult<List<RoleDto>>> GetRolesAsync(CancellationToken ct = default)
        {
            var roles = await _roleManager.Roles
                .AsNoTracking()
                .Select(r => new RoleDto(r.Id, r.Name))
                .ToListAsync(ct);

            return ServiceResult<List<RoleDto>>.Ok(roles);
        }

        public async Task<ServiceResult<RoleDto>> CreateRoleAsync(string name)
        {
            if (await _roleManager.RoleExistsAsync(name))
                return ServiceResult<RoleDto>.Conflict("Role already exists");

            var role = new IdentityRole(name);
            var result = await _roleManager.CreateAsync(role);

            if (!result.Succeeded)
            {
                _logger.LogError("Failed to create role {RoleName}: {Errors}",
                    name, string.Join("; ", result.Errors.Select(e => e.Description)));
                return ServiceResult<RoleDto>.Failed(result.Errors.Select(e => e.Description));
            }

            _logger.LogInformation("Role {RoleName} created", name);
            return ServiceResult<RoleDto>.Ok(new RoleDto(role.Id, role.Name));
        }

        public async Task<ServiceResult<List<UserDto>>> GetUsersAsync(CancellationToken ct = default)
        {
            // Consider adding paging (skip/take) here once the user table grows.
            var users = await _userManager.Users
                .AsNoTracking()
                .Select(u => new UserDto(u.Id, u.UserName, u.Email, u.EmailConfirmed))
                .ToListAsync(ct);

            return ServiceResult<List<UserDto>>.Ok(users);
        }

        public async Task<ServiceResult<IList<string>>> GetUserRolesAsync(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user is null)
                return ServiceResult<IList<string>>.NotFound("User doesn't exist");

            var roles = await _userManager.GetRolesAsync(user);
            return ServiceResult<IList<string>>.Ok(roles);
        }

        public async Task<ServiceResult<string>> AddUserToRoleAsync(string email, string roleName)
        {
            var (user, failure) = await ResolveUserAndRoleAsync(email, roleName);
            if (failure is not null) return failure;

            if (await _userManager.IsInRoleAsync(user!, roleName))
                return ServiceResult<string>.Conflict("User already has this role");

            var result = await _userManager.AddToRoleAsync(user!, roleName);
            if (!result.Succeeded)
                return ServiceResult<string>.Failed(result.Errors.Select(e => e.Description));

            _logger.LogInformation("User {UserId} added to role {RoleName}", user!.Id, roleName);
            return ServiceResult<string>.Ok("User has been added to the role");
        }

        public async Task<ServiceResult<string>> RemoveUserFromRoleAsync(string email, string roleName)
        {
            var (user, failure) = await ResolveUserAndRoleAsync(email, roleName);
            if (failure is not null) return failure;

            if (!await _userManager.IsInRoleAsync(user!, roleName))
                return ServiceResult<string>.NotFound("User does not have this role");

            var result = await _userManager.RemoveFromRoleAsync(user!, roleName);
            if (!result.Succeeded)
                return ServiceResult<string>.Failed(result.Errors.Select(e => e.Description));

            _logger.LogInformation("User {UserId} removed from role {RoleName}", user!.Id, roleName);
            return ServiceResult<string>.Ok("User has been removed from the role");
        }

        // The "user exists? role exists?" check was copy-pasted in two endpoints; it lives here once now.
        private async Task<(IdentityUser? User, ServiceResult<string>? Failure)> ResolveUserAndRoleAsync(
            string email, string roleName)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user is null)
                return (null, ServiceResult<string>.NotFound("User doesn't exist"));

            if (!await _roleManager.RoleExistsAsync(roleName))
                return (null, ServiceResult<string>.NotFound("Role doesn't exist"));

            return (user, null);
        }
    }
}