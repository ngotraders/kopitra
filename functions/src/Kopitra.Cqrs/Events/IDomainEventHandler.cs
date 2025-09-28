using System.Threading;
using System.Threading.Tasks;

namespace Kopitra.Cqrs.Events;

public interface IDomainEventHandler<TEvent> where TEvent : IDomainEvent
{
    Task HandleAsync(DomainEventEnvelope<TEvent> envelope, CancellationToken cancellationToken);
}
