using System.Threading.Tasks;

namespace Kopitra.Api.Application.Users.Services;

/// <summary>
/// Interface for authorization and permission checking
/// Enforces RBAC rules and resource ownership validation
/// </summary>
public interface IAuthorizationService
{
    /// <summary>
    /// Check if requesting user is admin
    /// </summary>
    Task<bool> IsAdminAsync(string requestingstring);

    /// <summary>
    /// Check if requesting user can manage target user (self or admin)
    /// </summary>
    Task<bool> CanManageUserAsync(string requestingstring, string targetstring);

    /// <summary>
    /// Check if requesting user can view target user data (self or admin)
    /// </summary>
    Task<bool> CanViewUserAsync(string requestingstring, string targetstring);

    /// <summary>
    /// Check if user has provider role
    /// </summary>
    bool HasProviderRole(string userId);

    /// <summary>
    /// Check if user has subscriber role
    /// </summary>
    bool HasSubscriberRole(string userId);

    /// <summary>
    /// Check if user is active
    /// </summary>
    bool IsUserActive(string userId);

    /// <summary>
    /// Check if requesting user can perform admin actions (permission changes, deactivation, etc.)
    /// </summary>
    Task<bool> CanPerformAdminActionsAsync(string requestingstring);
}
