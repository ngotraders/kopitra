using EventFlow.Aggregates.ExecutionResults;
using EventFlow.Commands;

namespace AdminApi.Domain.IdRegistries
{
    public class IdRegistryCommandHandler :
        ICommandHandler<IdRegistryAggregate, IdRegistryId, IExecutionResult, IdRegistryRegisterKeyCommand>,
        ICommandHandler<IdRegistryAggregate, IdRegistryId, IExecutionResult, IdRegistryRemoveKeyCommand>
    {
        public Task<IExecutionResult> ExecuteCommandAsync(IdRegistryAggregate aggregate, IdRegistryRegisterKeyCommand command, CancellationToken cancellationToken)
        {
            var executionResult = aggregate.AddKey(command.Key);
            return Task.FromResult(executionResult);
        }

        public Task<IExecutionResult> ExecuteCommandAsync(IdRegistryAggregate aggregate, IdRegistryRemoveKeyCommand command, CancellationToken cancellationToken)
        {
            var executionResult = aggregate.RemoveKeyIdPair(command.Key, command.IdForKey);
            return Task.FromResult(executionResult);
        }
    }
}
