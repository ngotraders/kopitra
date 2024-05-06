using EventFlow.Aggregates.ExecutionResults;
using EventFlow.Commands;

namespace AdminApi.Domain.DistributionGroups
{
    public class DistributionGroupAddAccountCommand :
        Command<DistributionGroupAggregate, DistributionGroupId, IExecutionResult>
    {
        public DistributionGroupAddAccountCommand(
            DistributionGroupId aggregateId,
            AccountId accountId)
            : base(aggregateId)
        {
            AccountId = accountId;
        }

        public AccountId AccountId { get; }
    }
}
