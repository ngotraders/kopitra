using EventFlow.Aggregates;
using EventFlow.ReadStores;
using Kopitra.Api.Domain.Users;
using Kopitra.Api.Domain.Users.Events;
using Kopitra.Api.Domain.ValueObjects;

namespace Kopitra.Api.Application.Users.Queries;

public class UserReadModel : IReadModel,
    IAmReadModelFor<UserAggregate, UserId, UserRegisteredEvent>,
    IAmReadModelFor<UserAggregate, UserId, UserLoginEvent>,
    IAmReadModelFor<UserAggregate, UserId, UserSettingsUpdatedEvent>,
    IAmReadModelFor<UserAggregate, UserId, UserProviderRoleEnabledEvent>,
    IAmReadModelFor<UserAggregate, UserId, UserProviderRoleDisabledEvent>,
    IAmReadModelFor<UserAggregate, UserId, UserDeactivatedEvent>,
    IAmReadModelFor<UserAggregate, UserId, UserSubscriberAccountCreatedEvent>,
    IAmReadModelFor<UserAggregate, UserId, UserRefreshTokenIssuedEvent>,
    IAmReadModelFor<UserAggregate, UserId, UserInfoUpdatedEvent>,
    IAmReadModelFor<UserAggregate, UserId, UserPermissionsChangedEvent>,
    IAmReadModelFor<UserAggregate, UserId, UserReactivatedEvent>,
    IAmReadModelFor<UserAggregate, UserId, UserAdminImpersonationStartedEvent>
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string[] Roles { get; set; } = Array.Empty<string>();
    public bool CanProvide { get; set; }
    public bool CanSubscribe { get; set; } = true;
    public DateTimeOffset RegisteredAt { get; set; }
    public DateTimeOffset? LastLoginAt { get; set; }
    public string? RefreshToken { get; set; }
    public DateTimeOffset? RefreshTokenExpiresAt { get; set; }
    public DateTimeOffset? LastAdminChangeAt { get; set; }
    public string? LastAdminChangeType { get; set; }

    public Task ApplyAsync(IReadModelContext context, IDomainEvent<UserAggregate, UserId, UserRegisteredEvent> domainEvent, CancellationToken cancellationToken)
    {
        var e = domainEvent.AggregateEvent;
        Id = context.ReadModelId;
        Email = e.Email;
        DisplayName = e.DisplayName;
        PasswordHash = e.PasswordHash;
        Roles = e.Roles;
        IsActive = true;
        RegisteredAt = domainEvent.Timestamp;
        return Task.CompletedTask;
    }

    public Task ApplyAsync(IReadModelContext context, IDomainEvent<UserAggregate, UserId, UserLoginEvent> domainEvent, CancellationToken cancellationToken)
    {
        LastLoginAt = domainEvent.Timestamp;
        return Task.CompletedTask;
    }

    public Task ApplyAsync(IReadModelContext context, IDomainEvent<UserAggregate, UserId, UserSettingsUpdatedEvent> domainEvent, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task ApplyAsync(IReadModelContext context, IDomainEvent<UserAggregate, UserId, UserProviderRoleEnabledEvent> domainEvent, CancellationToken cancellationToken)
    {
        Roles = Roles.Append("Provider").Distinct().ToArray();
        return Task.CompletedTask;
    }

    public Task ApplyAsync(IReadModelContext context, IDomainEvent<UserAggregate, UserId, UserProviderRoleDisabledEvent> domainEvent, CancellationToken cancellationToken)
    {
        Roles = Roles.Where(r => r != "Provider").ToArray();
        return Task.CompletedTask;
    }

    public Task ApplyAsync(IReadModelContext context, IDomainEvent<UserAggregate, UserId, UserDeactivatedEvent> domainEvent, CancellationToken cancellationToken)
    {
        IsActive = false;
        return Task.CompletedTask;
    }

    public Task ApplyAsync(IReadModelContext context, IDomainEvent<UserAggregate, UserId, UserSubscriberAccountCreatedEvent> domainEvent, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task ApplyAsync(IReadModelContext context, IDomainEvent<UserAggregate, UserId, UserRefreshTokenIssuedEvent> domainEvent, CancellationToken cancellationToken)
    {
        var e = domainEvent.AggregateEvent;
        RefreshToken = e.RefreshToken;
        RefreshTokenExpiresAt = domainEvent.Timestamp;
        return Task.CompletedTask;
    }

    public Task ApplyAsync(IReadModelContext context, IDomainEvent<UserAggregate, UserId, UserInfoUpdatedEvent> domainEvent, CancellationToken cancellationToken)
    {
        var e = domainEvent.AggregateEvent;
        if (!string.IsNullOrWhiteSpace(e.Email))
            Email = e.Email;
        if (!string.IsNullOrWhiteSpace(e.DisplayName))
            DisplayName = e.DisplayName;
        return Task.CompletedTask;
    }

    public Task ApplyAsync(IReadModelContext context, IDomainEvent<UserAggregate, UserId, UserPermissionsChangedEvent> domainEvent, CancellationToken cancellationToken)
    {
        var e = domainEvent.AggregateEvent;
        if (e.CanProvide.HasValue)
            CanProvide = e.CanProvide.Value;
        if (e.CanSubscribe.HasValue)
            CanSubscribe = e.CanSubscribe.Value;
        return Task.CompletedTask;
    }

    public Task ApplyAsync(IReadModelContext context, IDomainEvent<UserAggregate, UserId, UserReactivatedEvent> domainEvent, CancellationToken cancellationToken)
    {
        IsActive = true;
        return Task.CompletedTask;
    }

    public Task ApplyAsync(IReadModelContext context, IDomainEvent<UserAggregate, UserId, UserAdminImpersonationStartedEvent> domainEvent, CancellationToken cancellationToken)
    {
        // Event recorded for audit trail but doesn't change read model state
        return Task.CompletedTask;
    }
}
