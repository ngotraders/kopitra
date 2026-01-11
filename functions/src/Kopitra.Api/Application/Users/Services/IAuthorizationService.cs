using Kopitra.Api.Domain.ValueObjects;

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
    bool IsAdmin(UserId requestingUserId);

    /// <summary>
    /// Check if requesting user can manage target user (self or admin)
    /// </summary>
    bool CanManageUser(UserId requestingUserId, UserId targetUserId);

    /// <summary>
    /// Check if requesting user can view target user data (self or admin)
    /// </summary>
    bool CanViewUser(UserId requestingUserId, UserId targetUserId);

    /// <summary>
    /// Check if user has provider role
    /// </summary>
    bool HasProviderRole(UserId userId);

    /// <summary>
    /// Check if user has subscriber role
    /// </summary>
    bool HasSubscriberRole(UserId userId);

    /// <summary>
    /// Check if user is active
    /// </summary>
    bool IsUserActive(UserId userId);

    /// <summary>
    /// Check if requesting user can perform admin actions (permission changes, deactivation, etc.)
    /// </summary>
    bool CanPerformAdminActions(UserId requestingUserId);
}
