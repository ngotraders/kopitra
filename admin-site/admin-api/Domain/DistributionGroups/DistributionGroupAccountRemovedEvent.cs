using EventFlow.Aggregates;
using EventFlow.EventStores;

namespace AdminApi.Domain.DistributionGroups
{
    [EventVersion("DistributionGroupAccountRemoved", 1)]
    public class DistributionGroupAccountRemovedEvent :
       AggregateEvent<DistributionGroupAggregate, DistributionGroupId>
    {
        public DistributionGroupAccountRemovedEvent(AccountId accountId)
        {
            AccountId = accountId;
        }

        public AccountId AccountId { get; }
    }
}
