using Kopitra.Cqrs.Abstractions;

namespace Kopitra.Cqrs.EventStore;

public interface IAggregateStore
{
    Task<TAggregate> LoadAsync<TAggregate>(string aggregateId, CancellationToken cancellationToken)
        where TAggregate : EventSourcedAggregate, new();

    Task SaveAsync<TAggregate>(TAggregate aggregate, CancellationToken cancellationToken)
        where TAggregate : EventSourcedAggregate;
}
