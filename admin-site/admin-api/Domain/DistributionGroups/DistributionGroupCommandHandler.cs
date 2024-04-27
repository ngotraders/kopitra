using EventFlow.Aggregates.ExecutionResults;
using EventFlow.Commands;

namespace AdminApi.Domain.DistributionGroups
{
    public class DistributionGroupCommandHandler :
        ICommandHandler<IdRegistryAggregate, DistributionGroupId, IExecutionResult, DistributionGroupCreateCommand>,
        ICommandHandler<IdRegistryAggregate, DistributionGroupId, IExecutionResult, DistributionGroupUpdateCommand>,
        ICommandHandler<IdRegistryAggregate, DistributionGroupId, IExecutionResult, DistributionGroupDeleteCommand>
    {
        public Task<IExecutionResult> ExecuteCommandAsync(IdRegistryAggregate aggregate, DistributionGroupCreateCommand command, CancellationToken cancellationToken)
        {
            var executionResult = aggregate.SetName(command.Name);
            return Task.FromResult(executionResult);
        }
        public Task<IExecutionResult> ExecuteCommandAsync(IdRegistryAggregate aggregate, DistributionGroupUpdateCommand command, CancellationToken cancellationToken)
        {
            var executionResult = aggregate.SetName(command.Name);
            return Task.FromResult(executionResult);
        }
        public Task<IExecutionResult> ExecuteCommandAsync(IdRegistryAggregate aggregate, DistributionGroupDeleteCommand command, CancellationToken cancellationToken)
        {
            var executionResult = aggregate.Delete();
            return Task.FromResult(executionResult);
        }
    }
}
