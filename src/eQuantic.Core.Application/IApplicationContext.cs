namespace eQuantic.Core.Application;

public interface IApplicationContext : IApplicationContext<string>
{
}

public interface IApplicationContext<TUserKey>
{
    /// <summary>
    /// Get last update date of application.
    /// </summary>
    /// <value>
    /// The last update.
    /// </value>
    DateTime LastUpdate { get; }
    
    /// <summary>
    /// Get application version.
    /// </summary>
    /// <value>
    /// The version.
    /// </value>
    string? Version { get; }

    /// <summary>
    /// Get application local path.
    /// </summary>
    /// <value>
    /// The local path.
    /// </value>
    string? LocalPath { get; }
    
    /// <summary>
    /// Get current user
    /// </summary>
    /// <returns></returns>
    Task<TUserKey> GetCurrentUserIdAsync();
    
    /// <summary>
    /// Get current user roles
    /// </summary>
    /// <returns></returns>
    Task<string[]> GetCurrentUserRolesAsync();

    /// <summary>
    /// Verify if current user is in role
    /// </summary>
    /// <param name="role">The role name</param>
    /// <returns></returns>
    Task<bool> CurrentUserIsInRoleAsync(string role);
}