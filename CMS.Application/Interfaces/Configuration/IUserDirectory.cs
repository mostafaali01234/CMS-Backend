namespace CMS.Application.Interfaces.Configuration;

/// <summary>Questions about Identity users (roles, existence) that the Application layer cannot ask the DbContext directly.</summary>
public interface IUserDirectory
{
    /// <summary>ids of the users that have at least one of the roles</summary>
    Task<List<string>> GetUserIdsInRolesAsync(params string[] roles);
    Task<bool> UserExistsAsync(string userId);
}
