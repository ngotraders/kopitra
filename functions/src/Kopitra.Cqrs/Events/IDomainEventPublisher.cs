using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Kopitra.Cqrs.Events;

public interface IDomainEventPublisher
{
    Task PublishAsync(IEnumerable<IDomainEventEnvelope> events, CancellationToken cancellationToken);
}
