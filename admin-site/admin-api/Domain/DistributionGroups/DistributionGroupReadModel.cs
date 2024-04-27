using EventFlow.Aggregates;
using EventFlow.ReadStores;

namespace AdminApi.Domain.DistributionGroups
{
    public class DistributionGroupReadModel :
       IReadModel,
       IAmReadModelFor<IdRegistryAggregate, DistributionGroupId, DistributionGroupUpdatedEvent>,
       IAmReadModelFor<IdRegistryAggregate, DistributionGroupId, DistributionGroupDeletedEvent>
    {
        public string Name { get; private set; } = string.Empty;

        public Task ApplyAsync(IReadModelContext context, IDomainEvent<IdRegistryAggregate, DistributionGroupId, DistributionGroupUpdatedEvent> domainEvent, CancellationToken cancellationToken)
        {
            Name = domainEvent.AggregateEvent.Name;
            return Task.CompletedTask;
        }

        public Task ApplyAsync(IReadModelContext context, IDomainEvent<IdRegistryAggregate, DistributionGroupId, DistributionGroupDeletedEvent> domainEvent, CancellationToken cancellationToken)
        {
            context.MarkForDeletion();
            return Task.CompletedTask;
        }
    }
}
