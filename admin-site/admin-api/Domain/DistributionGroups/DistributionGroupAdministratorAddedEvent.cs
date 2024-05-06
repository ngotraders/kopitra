using EventFlow.Aggregates;
using EventFlow.EventStores;

namespace AdminApi.Domain.DistributionGroups
{
    [EventVersion("DistributionGroupAdministratorAdded", 1)]
    public class DistributionGroupAdministratorAddedEvent :
       AggregateEvent<DistributionGroupAggregate, DistributionGroupId>
    {
        public DistributionGroupAdministratorAddedEvent(UserId userId)
        {
            UserId = userId;
        }

        public UserId UserId { get; }
    }
}
