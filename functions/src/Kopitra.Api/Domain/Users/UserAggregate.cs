using EventFlow.Aggregates;
using Kopitra.Api.Domain.Users.Events;
using Kopitra.Api.Domain.ValueObjects;

namespace Kopitra.Api.Domain.Users;

/// <summary>
/// User aggregate - represents a user account with provider/subscriber roles
/// </summary>
public class UserAggregate : AggregateRoot<UserAggregate, UserId>
{
    public string Email { get; private set; } = null!;
    public string DisplayName { get; private set; } = null!;
    public string PasswordHash { get; private set; } = null!;
    public string[] Roles { get; private set; } = Array.Empty<string>();
    public bool IsProviderEnabled { get; private set; } = false;
    public bool IsActive { get; private set; } = true;
    public Dictionary<string, object> Settings { get; private set; } = new();

    public UserAggregate(UserId id) : base(id)
    {
    }

    /// <summary>
    /// Register new user
    /// </summary>
    public void Register(string email, string displayName, string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email is required.", nameof(email));
        if (string.IsNullOrWhiteSpace(displayName))
            throw new ArgumentException("Display name is required.", nameof(displayName));
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("Password hash is required.", nameof(passwordHash));

        Emit(new UserRegisteredEvent()
        {
            Email = email,
            DisplayName = displayName,
            PasswordHash = passwordHash,
            Roles = ["Subscriber"],
        });
    }

    /// <summary>
    /// Record user login
    /// </summary>
    public void RecordLogin()
    {
        if (!IsActive)
            throw new InvalidOperationException("User is not active.");

        Emit(new UserLoginEvent());
    }

    /// <summary>
    /// Update user settings
    /// </summary>
    public void UpdateSettings(Dictionary<string, object> settings)
    {
        if (settings == null || settings.Count == 0)
            throw new ArgumentException("Settings cannot be empty.", nameof(settings));

        Emit(new UserSettingsUpdatedEvent
        {
            Settings = settings,
        });
    }

    /// <summary>
    /// Enable provider role
    /// </summary>
    public void EnableProvider(string description)
    {
        if (IsProviderEnabled)
            throw new InvalidOperationException("Provider role is already enabled.");

        Emit(new UserProviderRoleEnabledEvent
        {
            ProviderDescription = description,
        });
    }

    /// <summary>
    /// Disable provider role
    /// </summary>
    public void DisableProvider(DateTime disabledAt)
    {
        if (!IsProviderEnabled)
            throw new InvalidOperationException("Provider role is not enabled.");

        Emit(new UserProviderRoleDisabledEvent
        {
        });
    }

    /// <summary>
    /// Deactivate user account
    /// </summary>
    public void Deactivate(string reason)
    {
        if (!IsActive)
            throw new InvalidOperationException("User is already deactivated.");

        Emit(new UserDeactivatedEvent
        {
            Reason = reason,
        });
    }

    /// <summary>
    /// Issue refresh token for this user
    /// </summary>
    public void IssueRefreshToken(string sessionId, string refreshToken, DateTimeOffset expiresAt)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
            throw new ArgumentException("Session id is required.", nameof(sessionId));
        if (string.IsNullOrWhiteSpace(refreshToken))
            throw new ArgumentException("Refresh token id is required.", nameof(refreshToken));

        if (!IsActive) throw new InvalidOperationException("User is not active.");

        Emit(new UserRefreshTokenIssuedEvent
        {
            SessionId =sessionId, 
            RefreshToken = refreshToken,
            ExpiresAt = expiresAt,
        });
    }

    /// <summary>
    /// Invalidate current refresh tokens for this user
    /// </summary>
    public void RevokeRefreshToken(string sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
            throw new ArgumentException("Session id is required.", nameof(sessionId));

        Emit(new UserRefreshTokenRevokedEvent()
        {
            SessionId = sessionId,
        });
    }

    /// <summary>
    /// Update user information (email, display name)
    /// </summary>
    public void UpdateInfo(string? email, string? displayName)
    {
        if (string.IsNullOrWhiteSpace(email) && string.IsNullOrWhiteSpace(displayName))
            throw new ArgumentException("At least one field must be provided.");

        Emit(new UserInfoUpdatedEvent
        {
            Email = email,
            DisplayName = displayName,
        });
    }

    /// <summary>
    /// Change user permissions (provider/subscriber rights)
    /// </summary>
    public void ChangePermissions(bool? canProvide, bool? canSubscribe)
    {
        if (canProvide == null && canSubscribe == null)
            throw new ArgumentException("At least one permission must be specified.");

        Emit(new UserPermissionsChangedEvent
        {
            CanProvide = canProvide,
            CanSubscribe = canSubscribe,
        });
    }

    /// <summary>
    /// Reactivate user account
    /// </summary>
    public void Reactivate()
    {
        if (IsActive)
            throw new InvalidOperationException("User is already active.");

        Emit(new UserReactivatedEvent
        {
        });
    }

    /// <summary>
    /// Record admin impersonation action for audit trail
    /// </summary>
    public void RecordAdminImpersonation(UserId adminUserId, string action, Dictionary<string, object> details)
    {
        if (adminUserId == null)
            throw new ArgumentException("Admin ID is required.", nameof(adminUserId));
        if (string.IsNullOrWhiteSpace(action))
            throw new ArgumentException("Action is required.", nameof(action));

        Emit(new UserAdminImpersonationStartedEvent
        {
            AdminUserId = adminUserId,
            Action = action,
            Details = details ?? new(),
        });
    }

    public void Apply(UserRegisteredEvent domainEvent)
    {
        Email = domainEvent.Email;
        DisplayName = domainEvent.DisplayName;
        PasswordHash = domainEvent.PasswordHash;
        Roles = domainEvent.Roles;
    }

    public void Apply(UserLoginEvent domainEvent)
    {
    }

    public void Apply(UserSettingsUpdatedEvent domainEvent)
    {
        Settings = domainEvent.Settings;
    }

    public void Apply(UserProviderRoleEnabledEvent domainEvent)
    {
        IsProviderEnabled = true;
        Roles = Roles.Append("Provider").Distinct().ToArray();
    }

    public void Apply(UserProviderRoleDisabledEvent domainEvent)
    {
        IsProviderEnabled = false;
        Roles = Roles.Where(r => r != "Provider").ToArray();
    }

    public void Apply(UserDeactivatedEvent domainEvent)
    {
        IsActive = false;
    }

    public void Apply(UserRefreshTokenIssuedEvent domainEvent)
    {
        // aggregate does not need to persist refresh token in-memory for loginAt
    }

    public void Apply(UserInfoUpdatedEvent domainEvent)
    {
        if (!string.IsNullOrWhiteSpace(domainEvent.Email))
            Email = domainEvent.Email;
        if (!string.IsNullOrWhiteSpace(domainEvent.DisplayName))
            DisplayName = domainEvent.DisplayName;
    }

    public void Apply(UserPermissionsChangedEvent domainEvent)
    {
        if (domainEvent.CanProvide.HasValue)
            IsProviderEnabled = domainEvent.CanProvide.Value;
        if (domainEvent.CanSubscribe.HasValue)
        {
            // Subscriber role handling (can always be toggled)
        }
    }

    public void Apply(UserReactivatedEvent domainEvent)
    {
        IsActive = true;
    }

    public void Apply(UserAdminImpersonationStartedEvent domainEvent)
    {
        // Event is recorded for audit trail
    }

    public void Apply(UserRefreshTokenRevokedEvent domainEvent)
    {
        // no in-memory state to update currently; token revocation is persisted elsewhere
    }
}