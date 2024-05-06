using EventFlow.Aggregates.ExecutionResults;
using EventFlow.Commands;

namespace AdminApi.Domain.DistributionGroups
{
    public class DistributionGroupUpdateAdministratorsCommand :
        Command<DistributionGroupAggregate, DistributionGroupId, IExecutionResult>
    {
        public DistributionGroupUpdateAdministratorsCommand(
            DistributionGroupId aggregateId,
            ICollection<UserId> administrators)
            : base(aggregateId)
        {
            Administrators = administrators;
        }

        public ICollection<UserId> Administrators { get; }
    }
}
