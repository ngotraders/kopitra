namespace Kopitra.Api.Application.Users.Services;

/// <summary>
/// Default implementation of authorization service
/// In production, this would check against a database of user roles and permissions
/// </summary>
public class AuthorizationService : IAuthorizationService
{
    // This would normally be injected with actual user repository
    private readonly HashSet<string> _admins = new();

    public AuthorizationService()
    {
        // In real implementation, load admin users from database
    }

    public bool IsAdmin(string requestingUserId)
    {
        return _admins.Contains(requestingUserId);
    }

    public bool CanManageUser(string requestingUserId, string targetUserId)
    {
        // User can manage themselves or admin can manage anyone
        return requestingUserId.Equals(targetUserId) || IsAdmin(requestingUserId);
    }

    public bool CanViewUser(string requestingUserId, string targetUserId)
    {
        // User can view themselves or admin can view anyone
        return requestingUserId.Equals(targetUserId) || IsAdmin(requestingUserId);
    }

    public bool HasProviderRole(string userId)
    {
        // Would check user read model in production
        return false;
    }

    public bool HasSubscriberRole(string userId)
    {
        // Would check user read model in production
        return true; // Default for new users
    }

    public bool IsUserActive(string userId)
    {
        // Would check user read model in production
        return true;
    }

    public bool CanPerformAdminActions(string requestingUserId)
    {
        return IsAdmin(requestingUserId);
    }

    /// <summary>
    /// Register a user as admin (for setup/testing)
    /// </summary>
    public void RegisterAdmin(string adminUserId)
    {
        _admins.Add(adminUserId);
    }

    /// <summary>
    /// Unregister admin (revoke admin rights)
    /// </summary>
    public void UnregisterAdmin(string adminUserId)
    {
        _admins.Remove(adminUserId);
    }
}
