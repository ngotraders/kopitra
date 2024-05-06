using EventFlow.Aggregates.ExecutionResults;
using EventFlow.Commands;

namespace AdminApi.Domain.DistributionGroups
{
    public class DistributionGroupDeleteCommand :
        Command<DistributionGroupAggregate, DistributionGroupId, IExecutionResult>
    {
        public DistributionGroupDeleteCommand(
            DistributionGroupId aggregateId)
            : base(aggregateId)
        {
        }
    }
}
