using System.Collections.Generic;
using Kopitra.Cqrs.Events;

namespace Kopitra.ManagementApi.Domain.AdminUsers;

public sealed record AdminUserProvisioned(
    string TenantId,
    string UserId,
    string Email,
    string DisplayName,
    IReadOnlyCollection<AdminUserRole> Roles,
    DateTimeOffset ProvisionedAt,
    string ProvisionedBy) : IDomainEvent;

public sealed record AdminUserRolesUpdated(
    string TenantId,
    string UserId,
    IReadOnlyCollection<AdminUserRole> Roles,
    DateTimeOffset UpdatedAt,
    string UpdatedBy) : IDomainEvent;

public sealed record AdminUserNotificationSettingsUpdated(
    string TenantId,
    string UserId,
    bool EmailEnabled,
    IReadOnlyCollection<string> Topics,
    DateTimeOffset UpdatedAt,
    string UpdatedBy) : IDomainEvent;
