using System.Threading;
using System.Threading.Tasks;
using EventFlow.Aggregates;
using EventFlow.Core;
using Kopitra.ManagementApi.Common.Cqrs;
using Kopitra.ManagementApi.Domain.ExpertAdvisors;
using Kopitra.ManagementApi.Infrastructure.ReadModels;
using Kopitra.ManagementApi.Time;

namespace Kopitra.ManagementApi.Application.ExpertAdvisors.Commands;

public sealed record RegisterExpertAdvisorCommand(
    string TenantId,
    string ExpertAdvisorId,
    string DisplayName,
    string Description,
    string RequestedBy) : ICommand<ExpertAdvisorReadModel>;

public sealed class RegisterExpertAdvisorCommandHandler : ICommandHandler<RegisterExpertAdvisorCommand, ExpertAdvisorReadModel>
{
    private readonly IAggregateStore _aggregateStore;
    private readonly IExpertAdvisorReadModelStore _readModelStore;
    private readonly IClock _clock;

    public RegisterExpertAdvisorCommandHandler(
        IAggregateStore aggregateStore,
        IExpertAdvisorReadModelStore readModelStore,
        IClock clock)
    {
        _aggregateStore = aggregateStore;
        _readModelStore = readModelStore;
        _clock = clock;
    }

    public async Task<ExpertAdvisorReadModel> HandleAsync(RegisterExpertAdvisorCommand command, CancellationToken cancellationToken)
    {
        var id = ExpertAdvisorId.FromBusinessId(command.ExpertAdvisorId);
        var timestamp = _clock.UtcNow;
        await _aggregateStore.UpdateAsync<ExpertAdvisorAggregate, ExpertAdvisorId>(
            id,
            SourceId.New,
            (aggregate, _) =>
            {
                aggregate.Register(command.TenantId, command.ExpertAdvisorId, command.DisplayName, command.Description, command.RequestedBy, timestamp);
                return Task.CompletedTask;
            },
            cancellationToken).ConfigureAwait(false);

        var readModel = await _readModelStore.GetAsync(command.TenantId, command.ExpertAdvisorId, cancellationToken).ConfigureAwait(false);
        if (readModel is not null)
        {
            return readModel;
        }

        var aggregateState = await _aggregateStore.LoadAsync<ExpertAdvisorAggregate, ExpertAdvisorId>(id, cancellationToken).ConfigureAwait(false);
        return new ExpertAdvisorReadModel(aggregateState.TenantId, command.ExpertAdvisorId, aggregateState.DisplayName, aggregateState.Description, aggregateState.Status, aggregateState.ApprovedBy, aggregateState.UpdatedAt);
    }
}
