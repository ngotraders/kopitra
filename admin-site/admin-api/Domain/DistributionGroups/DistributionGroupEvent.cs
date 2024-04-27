using EventFlow.Aggregates;
using EventFlow.EventStores;

namespace AdminApi.Domain.DistributionGroups
{
    [EventVersion("DistributionGroup", 1)]
    public class DistributionGroupEvent :
       AggregateEvent<DistributionGroupAggregate, DistributionGroupId>
    {
        public DistributionGroupEvent(int magicNumber)
        {
            MagicNumber = magicNumber;
        }

        public int MagicNumber { get; }
    }
}
