using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Kopitra.Cqrs.Commands;
using Kopitra.Cqrs.EventStore;
using Kopitra.ManagementApi.Domain.AdminUsers;
using Kopitra.ManagementApi.Infrastructure.ReadModels;
using Kopitra.ManagementApi.Time;

namespace Kopitra.ManagementApi.Application.Notifications.Commands;

public sealed record ConfigureAdminEmailNotificationsCommand(
    string TenantId,
    string UserId,
    bool EmailEnabled,
    IReadOnlyCollection<string> Topics,
    string RequestedBy) : ICommand<AdminUserReadModel>;

public sealed class ConfigureAdminEmailNotificationsCommandHandler : ICommandHandler<ConfigureAdminEmailNotificationsCommand, AdminUserReadModel>
{
    private readonly AggregateRepository<AdminUserAggregate, string> _repository;
    private readonly IAdminUserReadModelStore _readModelStore;
    private readonly IClock _clock;

    public ConfigureAdminEmailNotificationsCommandHandler(
        AggregateRepository<AdminUserAggregate, string> repository,
        IAdminUserReadModelStore readModelStore,
        IClock clock)
    {
        _repository = repository;
        _readModelStore = readModelStore;
        _clock = clock;
    }

    public async Task<AdminUserReadModel> HandleAsync(ConfigureAdminEmailNotificationsCommand command, CancellationToken cancellationToken)
    {
        var aggregate = await _repository.GetAsync(command.UserId, cancellationToken).ConfigureAwait(false);
        if (!string.Equals(aggregate.TenantId, command.TenantId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Tenant mismatch for admin notification configuration.");
        }

        aggregate.UpdateNotificationSettings(command.EmailEnabled, command.Topics, _clock.UtcNow, command.RequestedBy);
        await _repository.SaveAsync(aggregate, cancellationToken).ConfigureAwait(false);
        var readModel = await _readModelStore.GetAsync(command.TenantId, command.UserId, cancellationToken).ConfigureAwait(false);
        if (readModel is null)
        {
            throw new InvalidOperationException("Admin user read model missing after notification update.");
        }

        return readModel;
    }
}
