using EventFlow.Aggregates;
using EventFlow.Aggregates.ExecutionResults;
using EventFlow.Snapshots;
using EventFlow.Snapshots.Strategies;

namespace AdminApi.Domain.IdRegistries
{
    public class IdRegistryAggregate :
        SnapshotAggregateRoot<IdRegistryAggregate, IdRegistryId, IdRegistrySnapshot>,
        IEmit<IdRegistryKeyIdPairAddedEvent>,
        IEmit<IdRegistryKeyIdPairRemovedEvent>
    {
        public const int SnapshotEveryVersion = 10;

        private readonly Dictionary<string, string> _keyIdPairs = new();

        public IdRegistryAggregate(IdRegistryId id)
            : base(id, SnapshotEveryFewVersionsStrategy.With(SnapshotEveryVersion))
        {
        }

        public IExecutionResult AddKey(string key)
        {
            if (_keyIdPairs.ContainsKey(key))
            {
                return ExecutionResult.Failed("Specified key already exists.");
            }

            Emit(new IdRegistryKeyIdPairAddedEvent(key, Guid.NewGuid().ToString()));

            return ExecutionResult.Success();
        }

        public IExecutionResult RemoveKeyIdPair(string key, string generatedId)
        {
            if (!_keyIdPairs.TryGetValue(key, out var storedId))
            {
                return ExecutionResult.Failed("Specified key does not exists.");
            }
            if (storedId != generatedId)
            {
                return ExecutionResult.Failed("Stored id does not match.");
            }

            Emit(new IdRegistryKeyIdPairRemovedEvent(key, generatedId));

            return ExecutionResult.Success();
        }

        public void Apply(IdRegistryKeyIdPairAddedEvent aggregateEvent)
        {
            _keyIdPairs.Add(aggregateEvent.Key, aggregateEvent.IdForKey);
        }

        public void Apply(IdRegistryKeyIdPairRemovedEvent aggregateEvent)
        {
            _keyIdPairs.Remove(aggregateEvent.Key);
        }

        protected override Task<IdRegistrySnapshot> CreateSnapshotAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(new IdRegistrySnapshot(_keyIdPairs));
        }

        protected override Task LoadSnapshotAsync(IdRegistrySnapshot snapshot, ISnapshotMetadata metadata, CancellationToken cancellationToken)
        {
            _keyIdPairs.Clear();
            foreach (var item in snapshot.KeyIdPairs)
            {
                _keyIdPairs.Add(item.Value, item.Key);
            }
            return Task.CompletedTask;
        }
    }
}
