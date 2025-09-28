using System;
using System.Collections.Generic;
using System.Linq;
using EventFlow.Aggregates;

namespace Kopitra.ManagementApi.Domain.AdminUsers;

public sealed class AdminUserAggregate : AggregateRoot<AdminUserAggregate, AdminUserId>
{
    private readonly HashSet<AdminUserRole> _roles = new();
    private readonly HashSet<string> _topics = new(StringComparer.OrdinalIgnoreCase);

    public AdminUserAggregate(AdminUserId id) : base(id)
    {
    }

    public string TenantId { get; private set; } = string.Empty;
    public string BusinessId { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;
    public bool EmailEnabled { get; private set; }

    public IReadOnlyCollection<AdminUserRole> Roles => _roles;
    public IReadOnlyCollection<string> Topics => _topics;

    public void Provision(string tenantId, string userId, string email, string displayName, IEnumerable<AdminUserRole> roles, DateTimeOffset provisionedAt, string provisionedBy)
    {
        if (!string.IsNullOrEmpty(TenantId))
        {
            throw new InvalidOperationException($"Admin user {userId} already exists.");
        }

        Emit(new AdminUserProvisioned(tenantId, userId, email, displayName, roles.ToArray(), provisionedAt, provisionedBy));
    }

    public void UpdateRoles(IEnumerable<AdminUserRole> roles, DateTimeOffset updatedAt, string updatedBy)
    {
        var roleSet = roles.ToHashSet();
        if (roleSet.SetEquals(_roles))
        {
            return;
        }

        Emit(new AdminUserRolesUpdated(TenantId, BusinessId, roleSet, updatedAt, updatedBy));
    }

    public void UpdateNotificationSettings(bool emailEnabled, IEnumerable<string> topics, DateTimeOffset updatedAt, string updatedBy)
    {
        var topicSet = topics.Select(t => t.Trim()).Where(t => !string.IsNullOrEmpty(t)).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (EmailEnabled == emailEnabled && topicSet.SetEquals(_topics))
        {
            return;
        }

        Emit(new AdminUserNotificationSettingsUpdated(TenantId, BusinessId, emailEnabled, topicSet, updatedAt, updatedBy));
    }

    private void Apply(AdminUserProvisioned @event)
    {
        TenantId = @event.TenantId;
        BusinessId = @event.UserId;
        Email = @event.Email;
        DisplayName = @event.DisplayName;
        _roles.Clear();
        foreach (var role in @event.Roles)
        {
            _roles.Add(role);
        }
        EmailEnabled = false;
        _topics.Clear();
    }

    private void Apply(AdminUserRolesUpdated @event)
    {
        _roles.Clear();
        foreach (var role in @event.Roles)
        {
            _roles.Add(role);
        }
    }

    private void Apply(AdminUserNotificationSettingsUpdated @event)
    {
        EmailEnabled = @event.EmailEnabled;
        _topics.Clear();
        foreach (var topic in @event.Topics)
        {
            _topics.Add(topic);
        }
    }
}
