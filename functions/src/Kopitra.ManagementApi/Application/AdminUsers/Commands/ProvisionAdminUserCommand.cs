using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Kopitra.Cqrs.Commands;
using Kopitra.Cqrs.EventStore;
using Kopitra.ManagementApi.Domain.AdminUsers;
using Kopitra.ManagementApi.Infrastructure.ReadModels;
using Kopitra.ManagementApi.Time;

namespace Kopitra.ManagementApi.Application.AdminUsers.Commands;

public sealed record ProvisionAdminUserCommand(
    string TenantId,
    string UserId,
    string Email,
    string DisplayName,
    IReadOnlyCollection<AdminUserRole> Roles,
    string RequestedBy) : ICommand<AdminUserReadModel>;

public sealed class ProvisionAdminUserCommandHandler : ICommandHandler<ProvisionAdminUserCommand, AdminUserReadModel>
{
    private readonly AggregateRepository<AdminUserAggregate, string> _repository;
    private readonly IAdminUserReadModelStore _readModelStore;
    private readonly IClock _clock;

    public ProvisionAdminUserCommandHandler(
        AggregateRepository<AdminUserAggregate, string> repository,
        IAdminUserReadModelStore readModelStore,
        IClock clock)
    {
        _repository = repository;
        _readModelStore = readModelStore;
        _clock = clock;
    }

    public async Task<AdminUserReadModel> HandleAsync(ProvisionAdminUserCommand command, CancellationToken cancellationToken)
    {
        var aggregate = await _repository.GetAsync(command.UserId, cancellationToken).ConfigureAwait(false);
        if (!string.IsNullOrEmpty(aggregate.TenantId))
        {
            throw new InvalidOperationException($"Admin user {command.UserId} already exists for tenant {aggregate.TenantId}.");
        }

        aggregate.Provision(command.TenantId, command.UserId, command.Email, command.DisplayName, command.Roles, _clock.UtcNow, command.RequestedBy);
        await _repository.SaveAsync(aggregate, cancellationToken).ConfigureAwait(false);
        var readModel = await _readModelStore.GetAsync(command.TenantId, command.UserId, cancellationToken).ConfigureAwait(false);
        if (readModel is null)
        {
            throw new InvalidOperationException("Admin user read model missing after provisioning.");
        }

        return readModel;
    }
}
