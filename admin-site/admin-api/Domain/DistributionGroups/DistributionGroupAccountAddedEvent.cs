using EventFlow.Aggregates;
using EventFlow.EventStores;

namespace AdminApi.Domain.DistributionGroups
{
    [EventVersion("DistributionGroupAccountAdded", 1)]
    public class DistributionGroupAccountAddedEvent :
       AggregateEvent<DistributionGroupAggregate, DistributionGroupId>
    {
        public DistributionGroupAccountAddedEvent(AccountId accountId)
        {
            AccountId = accountId;
        }

        public AccountId AccountId { get; }
    }
}
