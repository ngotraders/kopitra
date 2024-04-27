using EventFlow.Aggregates.ExecutionResults;
using EventFlow.Aggregates;

namespace AdminApi.Domain.DistributionGroups
{
    public class DistributionGroupAggregate :
        AggregateRoot<DistributionGroupAggregate, DistributionGroupId>,
        IEmit<DistributionGroupEvent>
    {
        private int? _magicNumber;

        public DistributionGroupAggregate(DistributionGroupId id) : base(id) { }

        public IExecutionResult SetMagicNumer(int magicNumber)
        {
            if (_magicNumber.HasValue)
                return ExecutionResult.Failed("Magic number already set");

            Emit(new DistributionGroupEvent(magicNumber));

            return ExecutionResult.Success();
        }

        public void Apply(DistributionGroupEvent aggregateEvent)
        {
            _magicNumber = aggregateEvent.MagicNumber;
        }
    }
}
