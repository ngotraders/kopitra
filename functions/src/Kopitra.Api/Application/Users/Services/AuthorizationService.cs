using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Queries;
using Kopitra.Api.Application.Users.Queries;

namespace Kopitra.Api.Application.Users.Services;

/// <summary>
/// Default implementation of authorization service
/// In production, this would check against a database of user roles and permissions
/// </summary>
public class AuthorizationService : IAuthorizationService
{
    private readonly IQueryProcessor _queryProcessor;

    // Local admin cache for tests/setup; only a fallback
    private readonly HashSet<string> _admins = new();

    public AuthorizationService(IQueryProcessor queryProcessor)
    {
        _queryProcessor = queryProcessor ?? throw new ArgumentNullException(nameof(queryProcessor));
    }

    /// <summary>
    /// Check latest read model for whether the user is an admin (no caching)
    /// </summary>
    public async Task<bool> IsAdminAsync(string requestingUserId)
    {
        if (string.IsNullOrWhiteSpace(requestingUserId))
            return false;

        try
        {
            var admins = await _queryProcessor.ProcessAsync(new GetUsersByRoleQuery("admin"), CancellationToken.None).ConfigureAwait(false);
            if (admins != null && admins.Any(u => u.Id == requestingUserId))
                return true;
        }
        catch
        {
            // Fall back to local admin cache on errors
        }

        return _admins.Contains(requestingUserId);
    }

    public async Task<bool> CanManageUserAsync(string requestingUserId, string targetUserId)
    {
        if (requestingUserId == null) return false;
        if (requestingUserId.Equals(targetUserId)) return true;
        return await IsAdminAsync(requestingUserId).ConfigureAwait(false);
    }

    public async Task<bool> CanViewUserAsync(string requestingUserId, string targetUserId)
    {
        if (requestingUserId == null) return false;
        if (requestingUserId.Equals(targetUserId)) return true;
        return await IsAdminAsync(requestingUserId).ConfigureAwait(false);
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

    public Task<bool> CanPerformAdminActionsAsync(string requestingUserId)
    {
        return IsAdminAsync(requestingUserId);
    }

    /// <summary>
    /// Register a user as admin (for setup/testing)
    /// This only affects the local fallback cache; preferred source is the read model
    /// </summary>
    public void RegisterAdmin(string adminUserId)
    {
        if (!string.IsNullOrWhiteSpace(adminUserId))
            _admins.Add(adminUserId);
    }

    /// <summary>
    /// Unregister admin (revoke admin rights) from local cache
    /// </summary>
    public void UnregisterAdmin(string adminUserId)
    {
        if (!string.IsNullOrWhiteSpace(adminUserId))
            _admins.Remove(adminUserId);
    }
}
