using System.Threading;
using System.Threading.Tasks;
using Kopitra.ManagementApi.Common.Cqrs;
using Kopitra.ManagementApi.Domain.Integration;
using Kopitra.ManagementApi.Infrastructure.EventLog;
using Kopitra.ManagementApi.Time;

namespace Kopitra.ManagementApi.Application.Integration.Commands;

public sealed record RecordEaIntegrationEventCommand(
    string TenantId,
    string Source,
    string EventType,
    string Payload,
    DateTimeOffset OccurredAt) : ICommand<EaIntegrationEvent>;

public sealed class RecordEaIntegrationEventCommandHandler : ICommandHandler<RecordEaIntegrationEventCommand, EaIntegrationEvent>
{
    private readonly IEaIntegrationEventStore _eventStore;
    private readonly IClock _clock;

    public RecordEaIntegrationEventCommandHandler(IEaIntegrationEventStore eventStore, IClock clock)
    {
        _eventStore = eventStore;
        _clock = clock;
    }

    public async Task<EaIntegrationEvent> HandleAsync(RecordEaIntegrationEventCommand command, CancellationToken cancellationToken)
    {
        var integrationEvent = new EaIntegrationEvent(command.TenantId, command.Source, command.EventType, command.Payload, command.OccurredAt, _clock.UtcNow);
        await _eventStore.AppendAsync(integrationEvent, cancellationToken).ConfigureAwait(false);
        return integrationEvent;
    }
}
