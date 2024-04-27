using EventFlow.Aggregates.ExecutionResults;
using EventFlow.Commands;

namespace AdminApi.Domain.DistributionGroups
{
    public class DistributionGroupUpdateCommand :
        Command<IdRegistryAggregate, DistributionGroupId, IExecutionResult>
    {
        public DistributionGroupUpdateCommand(
            DistributionGroupId aggregateId,
            string name)
            : base(aggregateId)
        {
            Name = name;
        }

        public string Name { get; }
    }
}
