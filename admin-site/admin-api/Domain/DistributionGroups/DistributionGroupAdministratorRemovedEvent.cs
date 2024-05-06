using EventFlow.Aggregates;
using EventFlow.EventStores;

namespace AdminApi.Domain.DistributionGroups
{
    [EventVersion("DistributionGroupAdministratorRemoved", 1)]
    public class DistributionGroupAdministratorRemovedEvent :
       AggregateEvent<DistributionGroupAggregate, DistributionGroupId>
    {
        public DistributionGroupAdministratorRemovedEvent(UserId userId)
        {
            UserId = userId;
        }

        public UserId UserId { get; }
    }
}
