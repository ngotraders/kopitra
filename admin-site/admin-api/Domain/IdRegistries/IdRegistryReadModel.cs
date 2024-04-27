using EventFlow.Aggregates;
using EventFlow.ReadStores;

namespace AdminApi.Domain.IdRegistries
{
    public class IdRegistryReadModel :
       IReadModel,
       IAmReadModelFor<IdRegistryAggregate, IdRegistryId, IdRegistryKeyIdPairAddedEvent>,
       IAmReadModelFor<IdRegistryAggregate, IdRegistryId, IdRegistryKeyIdPairRemovedEvent>
    {
        private readonly Dictionary<string, string> _keyIdPairs = new();
        private readonly Dictionary<string, string> _idKeyPairs = new();

        public IReadOnlyDictionary<string, string> Keys => _keyIdPairs;
        public IReadOnlyDictionary<string, string> Ids => _idKeyPairs;

        public Task ApplyAsync(IReadModelContext context, IDomainEvent<IdRegistryAggregate, IdRegistryId, IdRegistryKeyIdPairAddedEvent> domainEvent, CancellationToken cancellationToken)
        {
            _keyIdPairs.Add(domainEvent.AggregateEvent.Key, domainEvent.AggregateEvent.IdForKey);
            _idKeyPairs.Add(domainEvent.AggregateEvent.IdForKey, domainEvent.AggregateEvent.Key);
            return Task.CompletedTask;
        }

        public Task ApplyAsync(IReadModelContext context, IDomainEvent<IdRegistryAggregate, IdRegistryId, IdRegistryKeyIdPairRemovedEvent> domainEvent, CancellationToken cancellationToken)
        {
            _keyIdPairs.Remove(domainEvent.AggregateEvent.Key);
            _idKeyPairs.Remove(domainEvent.AggregateEvent.IdForKey);
            return Task.CompletedTask;
        }
    }
}
