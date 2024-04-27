using EventFlow.Aggregates;
using EventFlow.EventStores;

namespace AdminApi.Domain.IdRegistries
{
    [EventVersion("IdRegistryKeyIdPairAddedEvent", 1)]
    public class IdRegistryKeyIdPairAddedEvent :
       AggregateEvent<IdRegistryAggregate, IdRegistryId>
    {
        public IdRegistryKeyIdPairAddedEvent(string key, string idForKey)
        {
            Key = key;
            IdForKey = idForKey;
        }

        public string Key { get; }
        public string IdForKey { get; }
    }
}
