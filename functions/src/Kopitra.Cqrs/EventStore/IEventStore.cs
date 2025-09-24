using Kopitra.Cqrs.Events;

namespace Kopitra.Cqrs.EventStore;

public interface IEventStore
{
    Task<IReadOnlyCollection<EventEnvelope>> LoadAsync(string streamId, CancellationToken cancellationToken);

    Task AppendAsync(string streamId, IReadOnlyCollection<EventEnvelope> events, int expectedVersion, CancellationToken cancellationToken);
}
