using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Aggregates;
using EventFlow.Core;
using Kopitra.ManagementApi.Common.Cqrs;
using Kopitra.ManagementApi.Domain.AdminUsers;
using Kopitra.ManagementApi.Infrastructure.AdminUsers;
using Kopitra.ManagementApi.Infrastructure.Authentication;
using Kopitra.ManagementApi.Infrastructure.ReadModels;
using Kopitra.ManagementApi.Time;

namespace Kopitra.ManagementApi.Application.AdminUsers.Commands;

public sealed record ProvisionAdminUserCommand(
    string TenantId,
    string UserId,
    string Email,
    string DisplayName,
    IReadOnlyCollection<AdminUserRole> Roles,
    string RequestedBy,
    string Password) : ICommand<AdminUserReadModel>;

public sealed class ProvisionAdminUserCommandHandler : ICommandHandler<ProvisionAdminUserCommand, AdminUserReadModel>
{
    private readonly IAggregateStore _aggregateStore;
    private readonly IAdminUserReadModelStore _readModelStore;
    private readonly IAdminUserCredentialStore _credentialStore;
    private readonly IClock _clock;

    public ProvisionAdminUserCommandHandler(
        IAggregateStore aggregateStore,
        IAdminUserReadModelStore readModelStore,
        IAdminUserCredentialStore credentialStore,
        IClock clock)
    {
        _aggregateStore = aggregateStore;
        _readModelStore = readModelStore;
        _credentialStore = credentialStore;
        _clock = clock;
    }

    public async Task<AdminUserReadModel> HandleAsync(ProvisionAdminUserCommand command, CancellationToken cancellationToken)
    {
        var userId = command.UserId.Trim();
        var email = command.Email.Trim();
        var displayName = command.DisplayName.Trim();
        var id = AdminUserId.FromBusinessId(userId);
        var timestamp = _clock.UtcNow;
        if (string.IsNullOrWhiteSpace(command.Password))
        {
            throw new InvalidOperationException("Password is required to provision an admin user.");
        }

        var passwordHash = PasswordHasher.HashPassword(command.Password);
        await _aggregateStore.UpdateAsync<AdminUserAggregate, AdminUserId>(
            id,
            SourceId.New,
            (aggregate, _) =>
            {
                if (!string.IsNullOrEmpty(aggregate.TenantId))
                {
                    throw new InvalidOperationException($"Admin user {command.UserId} already exists for tenant {aggregate.TenantId}.");
                }

                aggregate.Provision(command.TenantId, userId, email, displayName, command.Roles, timestamp, command.RequestedBy);
                return Task.CompletedTask;
            },
            cancellationToken).ConfigureAwait(false);

        await _credentialStore.SetAsync(
            new AdminUserCredential(
                command.TenantId,
                email,
                passwordHash,
                timestamp),
            cancellationToken).ConfigureAwait(false);

        var readModel = await _readModelStore.GetAsync(command.TenantId, userId, cancellationToken).ConfigureAwait(false);
        if (readModel is null)
        {
            var aggregateState = await _aggregateStore.LoadAsync<AdminUserAggregate, AdminUserId>(id, cancellationToken).ConfigureAwait(false);
            return new AdminUserReadModel(aggregateState.TenantId, userId, aggregateState.Email, aggregateState.DisplayName, aggregateState.Roles.ToArray(), aggregateState.EmailEnabled, aggregateState.Topics.ToArray(), timestamp);
        }

        return readModel;
    }
}
