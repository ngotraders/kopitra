using EventFlow.Aggregates;
using EventFlow.Aggregates.ExecutionResults;
using EventFlow.Snapshots;
using EventFlow.Snapshots.Strategies;

namespace AdminApi.Domain.DistributionGroups
{
    public class IdRegistryAggregate :
        SnapshotAggregateRoot<IdRegistryAggregate, DistributionGroupId, DistributionGroupSnapshot>,
        IEmit<DistributionGroupUpdatedEvent>,
        IEmit<DistributionGroupDeletedEvent>
    {
        public const int SnapshotEveryVersion = 10;

        private string? _name;
        private bool _isDeleted;

        public IdRegistryAggregate(DistributionGroupId id)
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
