using EventFlow.Aggregates;
using EventFlow.Aggregates.ExecutionResults;
using EventFlow.Snapshots;
using EventFlow.Snapshots.Strategies;

namespace AdminApi.Domain.DistributionGroups
{
    public class DistributionGroupAggregate :
        SnapshotAggregateRoot<DistributionGroupAggregate, DistributionGroupId, DistributionGroupSnapshot>,
        IEmit<DistributionGroupUpdatedEvent>,
        IEmit<DistributionGroupDeletedEvent>,
        IEmit<DistributionGroupAdministratorAddedEvent>,
        IEmit<DistributionGroupAdministratorRemovedEvent>
    {
        public const int SnapshotEveryVersion = 10;

        private string? _name;
        private readonly List<UserId> _administrators = new();
        private bool _isDeleted;

        public DistributionGroupAggregate(DistributionGroupId id)
            : base(id, SnapshotEveryFewVersionsStrategy.With(SnapshotEveryVersion))
        {
        }


        public IExecutionResult SetName(string name)
        {
            if (name == _name)
                return ExecutionResult.Success();

            Emit(new DistributionGroupUpdatedEvent(name));

            return ExecutionResult.Success();
        }

        public IExecutionResult SetAdministrators(ICollection<UserId> administrators)
        {
            var administratorsToAdd = administrators.Where(userId => !_administrators.Contains(userId)).ToList();
            var administratorsToRemove = _administrators.Where(userId => !administrators.Contains(userId)).ToList();
            foreach (var userId in administratorsToAdd)
            {
                Emit(new DistributionGroupAdministratorAddedEvent(userId));
            }
            foreach (var userId in administratorsToRemove)
            {
                Emit(new DistributionGroupAdministratorRemovedEvent(userId));
            }

            return ExecutionResult.Success();
        }

        public IExecutionResult Delete()
        {
            if (_isDeleted)
                return ExecutionResult.Failed("This aggregate has already deleted.");

            Emit(new DistributionGroupDeletedEvent());

            return ExecutionResult.Success();
        }

        public void Apply(DistributionGroupUpdatedEvent aggregateEvent)
        {
            _name = aggregateEvent.Name;
        }

        public void Apply(DistributionGroupDeletedEvent aggregateEvent)
        {
            _isDeleted = true;
        }

        public void Apply(DistributionGroupAdministratorAddedEvent aggregateEvent)
        {
            _administrators.Add(aggregateEvent.UserId);
        }

        public void Apply(DistributionGroupAdministratorRemovedEvent aggregateEvent)
        {
            _administrators.Remove(aggregateEvent.UserId);
        }

        protected override Task<DistributionGroupSnapshot> CreateSnapshotAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(new DistributionGroupSnapshot(_name!));
        }

        protected override Task LoadSnapshotAsync(DistributionGroupSnapshot snapshot, ISnapshotMetadata metadata, CancellationToken cancellationToken)
        {
            _name = snapshot.Name;
            return Task.CompletedTask;
        }
    }
}
