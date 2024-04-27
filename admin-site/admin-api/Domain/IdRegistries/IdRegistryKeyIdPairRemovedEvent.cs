using EventFlow.Aggregates;

namespace AdminApi.Domain.IdRegistries
{
    public class IdRegistryKeyIdPairRemovedEvent :
       AggregateEvent<IdRegistryAggregate, IdRegistryId>
    {
        public IdRegistryKeyIdPairRemovedEvent(string key, string idForKey)
        {
            Key = key;
            IdForKey = idForKey;
        }

        public string Key { get; }
        public string IdForKey { get; }
    }
}
