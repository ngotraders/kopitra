using EventFlow.Aggregates.ExecutionResults;
using EventFlow.Commands;

namespace AdminApi.Domain.DistributionGroups
{
    public class DistributionGroupCommandHandler :
        ICommandHandler<DistributionGroupAggregate, DistributionGroupId, IExecutionResult, DistributionGroupCreateCommand>,
        ICommandHandler<DistributionGroupAggregate, DistributionGroupId, IExecutionResult, DistributionGroupDeleteCommand>,
        ICommandHandler<DistributionGroupAggregate, DistributionGroupId, IExecutionResult, DistributionGroupUpdateCommand>,
        ICommandHandler<DistributionGroupAggregate, DistributionGroupId, IExecutionResult, DistributionGroupUpdateAdministratorsCommand>
    {
        public Task<IExecutionResult> ExecuteCommandAsync(DistributionGroupAggregate aggregate, DistributionGroupCreateCommand command, CancellationToken cancellationToken)
        {
            var executionResult = aggregate.SetName(command.Name);
            return Task.FromResult(executionResult);
        }

        public Task<IExecutionResult> ExecuteCommandAsync(DistributionGroupAggregate aggregate, DistributionGroupDeleteCommand command, CancellationToken cancellationToken)
        {
            var executionResult = aggregate.Delete();
            return Task.FromResult(executionResult);
        }

        public Task<IExecutionResult> ExecuteCommandAsync(DistributionGroupAggregate aggregate, DistributionGroupUpdateCommand command, CancellationToken cancellationToken)
        {
            var executionResult = aggregate.SetName(command.Name);
            return Task.FromResult(executionResult);
        }

        public Task<IExecutionResult> ExecuteCommandAsync(DistributionGroupAggregate aggregate, DistributionGroupUpdateAdministratorsCommand command, CancellationToken cancellationToken)
        {
            var executionResult = aggregate.SetAdministrators(command.Administrators);
            return Task.FromResult(executionResult);
        }
    }
}
