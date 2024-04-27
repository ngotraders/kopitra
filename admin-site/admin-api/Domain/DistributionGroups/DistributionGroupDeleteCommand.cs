using EventFlow.Aggregates.ExecutionResults;
using EventFlow.Commands;

namespace AdminApi.Domain.DistributionGroups
{
    public class DistributionGroupDeleteCommand :
        Command<IdRegistryAggregate, DistributionGroupId, IExecutionResult>
    {
        public DistributionGroupDeleteCommand(
            DistributionGroupId aggregateId)
            : base(aggregateId)
        {
        }
    }
}
