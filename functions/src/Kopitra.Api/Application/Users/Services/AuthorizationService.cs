using Kopitra.Api.Domain.ValueObjects;

namespace Kopitra.Api.Application.Users.Services;

/// <summary>
/// Default implementation of authorization service
/// In production, this would check against a database of user roles and permissions
/// </summary>
public class AuthorizationService : IAuthorizationService
{
    // This would normally be injected with actual user repository
    private readonly HashSet<UserId> _admins = new();

    public AuthorizationService()
    {
        // In real implementation, load admin users from database
    }

    public bool IsAdmin(UserId requestingUserId)
    {
        return _admins.Contains(requestingUserId);
    }

    public bool CanManageUser(UserId requestingUserId, UserId targetUserId)
    {
        // User can manage themselves or admin can manage anyone
        return requestingUserId.Equals(targetUserId) || IsAdmin(requestingUserId);
    }

    public bool CanViewUser(UserId requestingUserId, UserId targetUserId)
    {
        // User can view themselves or admin can view anyone
        return requestingUserId.Equals(targetUserId) || IsAdmin(requestingUserId);
    }

    public bool HasProviderRole(UserId userId)
    {
        // Would check user read model in production
        return false;
    }

    public bool HasSubscriberRole(UserId userId)
    {
        // Would check user read model in production
        return true; // Default for new users
    }

    public bool IsUserActive(UserId userId)
    {
        // Would check user read model in production
        return true;
    }

    public bool CanPerformAdminActions(UserId requestingUserId)
    {
        return IsAdmin(requestingUserId);
    }

    /// <summary>
    /// Register a user as admin (for setup/testing)
    /// </summary>
    public void RegisterAdmin(UserId adminUserId)
    {
        _admins.Add(adminUserId);
    }

    /// <summary>
    /// Unregister admin (revoke admin rights)
    /// </summary>
    public void UnregisterAdmin(UserId adminUserId)
    {
        _admins.Remove(adminUserId);
    }
}
