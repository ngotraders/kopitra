using EventFlow.Aggregates;
using EventFlow.EventStores;

namespace AdminApi.Domain.DistributionGroups
{
    [EventVersion("DistributionGroupUpdatedEvent", 1)]
    public class DistributionGroupUpdatedEvent :
       AggregateEvent<DistributionGroupAggregate, DistributionGroupId>
    {
        public DistributionGroupUpdatedEvent(string name)
        {
            Name = name;
        }

        public string Name { get; }
    }
}
