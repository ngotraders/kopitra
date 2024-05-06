using System.Collections;
using EventFlow.Aggregates;
using EventFlow.ReadStores;

namespace AdminApi.Domain.DistributionGroups
{
    public class DistributionGroupReadModel :
       IReadModel,
       IAmReadModelFor<DistributionGroupAggregate, DistributionGroupId, DistributionGroupUpdatedEvent>,
       IAmReadModelFor<DistributionGroupAggregate, DistributionGroupId, DistributionGroupDeletedEvent>,
       IAmReadModelFor<DistributionGroupAggregate, DistributionGroupId, DistributionGroupAdministratorAddedEvent>,
       IAmReadModelFor<DistributionGroupAggregate, DistributionGroupId, DistributionGroupAdministratorRemovedEvent>
    {
        public string Name { get; private set; } = string.Empty;

        private readonly List<UserId> _Administrators = new();
        public IReadOnlyList<UserId> Administrators => _Administrators;

        public Task ApplyAsync(IReadModelContext context, IDomainEvent<DistributionGroupAggregate, DistributionGroupId, DistributionGroupUpdatedEvent> domainEvent, CancellationToken cancellationToken)
        {
            Name = domainEvent.AggregateEvent.Name;
            return Task.CompletedTask;
        }

        public Task ApplyAsync(IReadModelContext context, IDomainEvent<DistributionGroupAggregate, DistributionGroupId, DistributionGroupDeletedEvent> domainEvent, CancellationToken cancellationToken)
        {
            context.MarkForDeletion();
            return Task.CompletedTask;
        }

        public Task ApplyAsync(IReadModelContext context, IDomainEvent<DistributionGroupAggregate, DistributionGroupId, DistributionGroupAdministratorAddedEvent> domainEvent, CancellationToken cancellationToken)
        {
            _Administrators.Add(domainEvent.AggregateEvent.UserId);
            return Task.CompletedTask;
        }

        public Task ApplyAsync(IReadModelContext context, IDomainEvent<DistributionGroupAggregate, DistributionGroupId, DistributionGroupAdministratorRemovedEvent> domainEvent, CancellationToken cancellationToken)
        {
            _Administrators.Remove(domainEvent.AggregateEvent.UserId);
            return Task.CompletedTask;
        }
    }
}
