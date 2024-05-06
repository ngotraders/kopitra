using EventFlow.Aggregates;
using EventFlow.EventStores;

namespace AdminApi.Domain.DistributionGroups
{
    [EventVersion("DistributionGroupDeleted", 1)]
    public class DistributionGroupDeletedEvent :
       AggregateEvent<DistributionGroupAggregate, DistributionGroupId>
    {
    }
}
