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
        IEmit<DistributionGroupAdministratorRemovedEvent>,
        IEmit<DistributionGroupAccountAddedEvent>,
        IEmit<DistributionGroupAccountRemovedEvent>
    {
        public const int SnapshotEveryVersion = 10;

        private string? _name;
        private readonly List<UserId> _administrators = new();
        private readonly List<AccountId> _accounts = new();
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

        public IExecutionResult AddAccount(AccountId accountId)
        {
            if (_accounts.Contains(accountId))
                return ExecutionResult.Failed("このアカウントはすでに登録されています。");
            Emit(new DistributionGroupAccountAddedEvent(accountId));
            return ExecutionResult.Success();
        }

        public IExecutionResult RemoveAccount(AccountId accountId)
        {
            if (!_accounts.Contains(accountId))
                return ExecutionResult.Failed("このアカウントはこの配信グループに所属していません。");
            Emit(new DistributionGroupAccountRemovedEvent(accountId));
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

        public void Apply(DistributionGroupAccountAddedEvent aggregateEvent)
        {
            _accounts.Add(aggregateEvent.AccountId);
        }

        public void Apply(DistributionGroupAccountRemovedEvent aggregateEvent)
        {
            _accounts.Remove(aggregateEvent.AccountId);
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
