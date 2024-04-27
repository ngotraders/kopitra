using EventFlow.Aggregates;
using EventFlow.ReadStores;

namespace AdminApi.Domain.DistributionGroups
{
    public class DistributionGroupReadModel :
       IReadModel,
       IAmReadModelFor<DistributionGroupAggregate, DistributionGroupId, DistributionGroupEvent>
    {
        public int MagicNumber { get; private set; }

        public Task ApplyAsync(IReadModelContext context, IDomainEvent<DistributionGroupAggregate, DistributionGroupId, DistributionGroupEvent> domainEvent, CancellationToken cancellationToken)
        {
            MagicNumber = domainEvent.AggregateEvent.MagicNumber;
            return Task.CompletedTask;
        }
    }
}
