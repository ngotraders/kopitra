using EventFlow.Aggregates.ExecutionResults;
using EventFlow.Commands;

namespace AdminApi.Domain.IdRegistries
{
    public class IdRegistryRemoveKeyCommand :
        Command<IdRegistryAggregate, IdRegistryId, IExecutionResult>
    {
        public IdRegistryRemoveKeyCommand(
            IdRegistryId aggregateId,
            string key,
            string idForKey)
            : base(aggregateId)
        {
            Key = key;
            IdForKey = idForKey;
        }

        public string Key { get; }
        public string IdForKey { get; }
    }
}
