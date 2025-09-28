using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Kopitra.Cqrs.Events;

namespace Kopitra.Cqrs.EventStore;

public interface IEventStore
{
    Task<IReadOnlyCollection<IDomainEventEnvelope>> LoadAsync(string streamId, CancellationToken cancellationToken);

    Task AppendAsync(string streamId, int expectedVersion, IReadOnlyCollection<IDomainEventEnvelope> events, CancellationToken cancellationToken);
}
