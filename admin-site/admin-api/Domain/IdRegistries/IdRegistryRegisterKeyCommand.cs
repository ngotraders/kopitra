using EventFlow.Aggregates.ExecutionResults;
using EventFlow.Commands;

namespace AdminApi.Domain.IdRegistries
{
    public class IdRegistryRegisterKeyCommand :
        Command<IdRegistryAggregate, IdRegistryId, IExecutionResult>
    {
        public IdRegistryRegisterKeyCommand(
            IdRegistryId aggregateId,
            string key)
            : base(aggregateId)
        {
            Key = key;
        }

        public string Key { get; }
    }
}
