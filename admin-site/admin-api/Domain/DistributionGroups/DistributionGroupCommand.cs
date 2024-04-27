using EventFlow.Aggregates.ExecutionResults;
using EventFlow.Commands;

namespace AdminApi.Domain.DistributionGroups
{
    public class DistributionGroupCommand :
        Command<DistributionGroupAggregate, DistributionGroupId, IExecutionResult>
    {
        public DistributionGroupCommand(
            DistributionGroupId aggregateId,
            int magicNumber)
            : base(aggregateId)
        {
            MagicNumber = magicNumber;
        }

        public int MagicNumber { get; }
    }
}
