using EventFlow.Aggregates.ExecutionResults;
using EventFlow.Commands;

namespace AdminApi.Domain.DistributionGroups
{
    public class DistributionGroupCommandHandler :
        CommandHandler<DistributionGroupAggregate, DistributionGroupId, IExecutionResult, DistributionGroupCommand>
    {
        public override Task<IExecutionResult> ExecuteCommandAsync(
            DistributionGroupAggregate aggregate,
            DistributionGroupCommand command,
            CancellationToken cancellationToken)
        {
            var executionResult = aggregate.SetMagicNumer(command.MagicNumber);
            return Task.FromResult(executionResult);
        }
    }
}
