using System;
using System.Collections.Generic;
using EventFlow.Aggregates;
using EventFlow.EventStores;

namespace Kopitra.ManagementApi.Domain.AdminUsers;

[EventVersion("admin-user-provisioned", 1)]
public sealed class AdminUserProvisioned : AggregateEvent<AdminUserAggregate, AdminUserId>
{
    public AdminUserProvisioned(string tenantId, string userId, string email, string displayName, IReadOnlyCollection<AdminUserRole> roles, DateTimeOffset provisionedAt, string provisionedBy)
    {
        TenantId = tenantId;
        UserId = userId;
        Email = email;
        DisplayName = displayName;
        Roles = roles;
        ProvisionedAt = provisionedAt;
        ProvisionedBy = provisionedBy;
    }

    public string TenantId { get; }
    public string UserId { get; }
    public string Email { get; }
    public string DisplayName { get; }
    public IReadOnlyCollection<AdminUserRole> Roles { get; }
    public DateTimeOffset ProvisionedAt { get; }
    public string ProvisionedBy { get; }
}

[EventVersion("admin-user-roles-updated", 1)]
public sealed class AdminUserRolesUpdated : AggregateEvent<AdminUserAggregate, AdminUserId>
{
    public AdminUserRolesUpdated(string tenantId, string userId, IReadOnlyCollection<AdminUserRole> roles, DateTimeOffset updatedAt, string updatedBy)
    {
        TenantId = tenantId;
        UserId = userId;
        Roles = roles;
        UpdatedAt = updatedAt;
        UpdatedBy = updatedBy;
    }

    public string TenantId { get; }
    public string UserId { get; }
    public IReadOnlyCollection<AdminUserRole> Roles { get; }
    public DateTimeOffset UpdatedAt { get; }
    public string UpdatedBy { get; }
}

[EventVersion("admin-user-notification-settings-updated", 1)]
public sealed class AdminUserNotificationSettingsUpdated : AggregateEvent<AdminUserAggregate, AdminUserId>
{
    public AdminUserNotificationSettingsUpdated(string tenantId, string userId, bool emailEnabled, IReadOnlyCollection<string> topics, DateTimeOffset updatedAt, string updatedBy)
    {
        TenantId = tenantId;
        UserId = userId;
        EmailEnabled = emailEnabled;
        Topics = topics;
        UpdatedAt = updatedAt;
        UpdatedBy = updatedBy;
    }

    public string TenantId { get; }
    public string UserId { get; }
    public bool EmailEnabled { get; }
    public IReadOnlyCollection<string> Topics { get; }
    public DateTimeOffset UpdatedAt { get; }
    public string UpdatedBy { get; }
}
