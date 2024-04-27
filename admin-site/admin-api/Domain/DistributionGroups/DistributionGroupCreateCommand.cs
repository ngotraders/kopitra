using EventFlow.Aggregates.ExecutionResults;
using EventFlow.Commands;

namespace AdminApi.Domain.DistributionGroups
{
    public class DistributionGroupCreateCommand :
        Command<IdRegistryAggregate, DistributionGroupId, IExecutionResult>
    {
        public DistributionGroupCreateCommand(
            DistributionGroupId aggregateId,
            string name)
            : base(aggregateId)
        {
            Name = name;
        }

        public string Name { get; }
    }
}
