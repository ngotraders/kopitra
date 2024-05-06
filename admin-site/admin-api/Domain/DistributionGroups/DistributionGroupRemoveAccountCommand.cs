using EventFlow.Aggregates.ExecutionResults;
using EventFlow.Commands;

namespace AdminApi.Domain.DistributionGroups
{
    public class DistributionGroupRemoveAccountCommand :
        Command<DistributionGroupAggregate, DistributionGroupId, IExecutionResult>
    {
        public DistributionGroupRemoveAccountCommand(
            DistributionGroupId aggregateId,
            AccountId accountId)
            : base(aggregateId)
        {
            AccountId = accountId;
        }

        public AccountId AccountId { get; }
    }
}
