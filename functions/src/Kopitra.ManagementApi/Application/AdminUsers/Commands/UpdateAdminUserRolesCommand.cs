using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Aggregates;
using EventFlow.Core;
using Kopitra.ManagementApi.Common.Cqrs;
using Kopitra.ManagementApi.Domain.AdminUsers;
using Kopitra.ManagementApi.Infrastructure.ReadModels;
using Kopitra.ManagementApi.Time;

namespace Kopitra.ManagementApi.Application.AdminUsers.Commands;

public sealed record UpdateAdminUserRolesCommand(
    string TenantId,
    string UserId,
    IReadOnlyCollection<AdminUserRole> Roles,
    string RequestedBy) : ICommand<AdminUserReadModel>;

public sealed class UpdateAdminUserRolesCommandHandler : ICommandHandler<UpdateAdminUserRolesCommand, AdminUserReadModel>
{
    private readonly IAggregateStore _aggregateStore;
    private readonly IAdminUserReadModelStore _readModelStore;
    private readonly IClock _clock;

    public UpdateAdminUserRolesCommandHandler(
        IAggregateStore aggregateStore,
        IAdminUserReadModelStore readModelStore,
        IClock clock)
    {
        _aggregateStore = aggregateStore;
        _readModelStore = readModelStore;
        _clock = clock;
    }

    public async Task<AdminUserReadModel> HandleAsync(UpdateAdminUserRolesCommand command, CancellationToken cancellationToken)
    {
        var id = AdminUserId.FromBusinessId(command.UserId);
        var timestamp = _clock.UtcNow;
        await _aggregateStore.UpdateAsync<AdminUserAggregate, AdminUserId>(
            id,
            SourceId.New,
            (aggregate, _) =>
            {
                if (!string.Equals(aggregate.TenantId, command.TenantId, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException("Tenant mismatch for admin user role update.");
                }

                aggregate.UpdateRoles(command.Roles, timestamp, command.RequestedBy);
                return Task.CompletedTask;
            },
            cancellationToken).ConfigureAwait(false);

        var readModel = await _readModelStore.GetAsync(command.TenantId, command.UserId, cancellationToken).ConfigureAwait(false);
        if (readModel is null)
        {
            var aggregateState = await _aggregateStore.LoadAsync<AdminUserAggregate, AdminUserId>(id, cancellationToken).ConfigureAwait(false);
            return new AdminUserReadModel(aggregateState.TenantId, command.UserId, aggregateState.Email, aggregateState.DisplayName, aggregateState.Roles.ToArray(), aggregateState.EmailEnabled, aggregateState.Topics.ToArray(), timestamp);
        }

        return readModel;
    }
}
