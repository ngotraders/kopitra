using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Kopitra.Cqrs.Commands;
using Kopitra.Cqrs.EventStore;
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
    private readonly AggregateRepository<AdminUserAggregate, string> _repository;
    private readonly IAdminUserReadModelStore _readModelStore;
    private readonly IClock _clock;

    public UpdateAdminUserRolesCommandHandler(
        AggregateRepository<AdminUserAggregate, string> repository,
        IAdminUserReadModelStore readModelStore,
        IClock clock)
    {
        _repository = repository;
        _readModelStore = readModelStore;
        _clock = clock;
    }

    public async Task<AdminUserReadModel> HandleAsync(UpdateAdminUserRolesCommand command, CancellationToken cancellationToken)
    {
        var aggregate = await _repository.GetAsync(command.UserId, cancellationToken).ConfigureAwait(false);
        if (!string.Equals(aggregate.TenantId, command.TenantId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Tenant mismatch for admin user role update.");
        }

        aggregate.UpdateRoles(command.Roles, _clock.UtcNow, command.RequestedBy);
        await _repository.SaveAsync(aggregate, cancellationToken).ConfigureAwait(false);
        var readModel = await _readModelStore.GetAsync(command.TenantId, command.UserId, cancellationToken).ConfigureAwait(false);
        if (readModel is null)
        {
            throw new InvalidOperationException("Admin user read model missing after role update.");
        }

        return readModel;
    }
}
