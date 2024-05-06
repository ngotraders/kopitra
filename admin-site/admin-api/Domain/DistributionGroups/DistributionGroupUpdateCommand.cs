using EventFlow.Aggregates.ExecutionResults;
using EventFlow.Commands;

namespace AdminApi.Domain.DistributionGroups
{
    public class DistributionGroupUpdateCommand :
        Command<DistributionGroupAggregate, DistributionGroupId, IExecutionResult>
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
