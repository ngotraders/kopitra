using System;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Aggregates;
using EventFlow.Core;
using Kopitra.ManagementApi.Common.Cqrs;
using Kopitra.ManagementApi.Domain.ExpertAdvisors;
using Kopitra.ManagementApi.Infrastructure.ReadModels;
using Kopitra.ManagementApi.Time;

namespace Kopitra.ManagementApi.Application.ExpertAdvisors.Commands;

public sealed record UpdateExpertAdvisorStatusCommand(
    string TenantId,
    string ExpertAdvisorId,
    ExpertAdvisorStatus Status,
    string? Reason,
    string RequestedBy) : ICommand<ExpertAdvisorReadModel>;

public sealed class UpdateExpertAdvisorStatusCommandHandler : ICommandHandler<UpdateExpertAdvisorStatusCommand, ExpertAdvisorReadModel>
{
    private readonly IAggregateStore _aggregateStore;
    private readonly IExpertAdvisorReadModelStore _readModelStore;
    private readonly IClock _clock;

    public UpdateExpertAdvisorStatusCommandHandler(
        IAggregateStore aggregateStore,
        IExpertAdvisorReadModelStore readModelStore,
        IClock clock)
    {
        _aggregateStore = aggregateStore;
        _readModelStore = readModelStore;
        _clock = clock;
    }

    public async Task<ExpertAdvisorReadModel> HandleAsync(UpdateExpertAdvisorStatusCommand command, CancellationToken cancellationToken)
    {
        var id = ExpertAdvisorId.FromBusinessId(command.ExpertAdvisorId);
        var timestamp = _clock.UtcNow;
        await _aggregateStore.UpdateAsync<ExpertAdvisorAggregate, ExpertAdvisorId>(
            id,
            SourceId.New,
            (aggregate, _) =>
            {
                if (!string.Equals(aggregate.TenantId, command.TenantId, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException("Tenant mismatch for expert advisor status change.");
                }

                aggregate.ChangeStatus(command.Status, command.Reason, timestamp);
                return Task.CompletedTask;
            },
            cancellationToken).ConfigureAwait(false);

        var readModel = await _readModelStore.GetAsync(command.TenantId, command.ExpertAdvisorId, cancellationToken).ConfigureAwait(false);
        if (readModel is null)
        {
            var aggregateState = await _aggregateStore.LoadAsync<ExpertAdvisorAggregate, ExpertAdvisorId>(id, cancellationToken).ConfigureAwait(false);
            return new ExpertAdvisorReadModel(aggregateState.TenantId, command.ExpertAdvisorId, aggregateState.DisplayName, aggregateState.Description, aggregateState.Status, aggregateState.ApprovedBy, aggregateState.UpdatedAt);
        }

        return readModel;
    }
}
