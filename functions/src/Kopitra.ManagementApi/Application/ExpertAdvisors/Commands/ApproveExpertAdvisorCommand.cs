using System.Threading;
using System.Threading.Tasks;
using Kopitra.Cqrs.Commands;
using Kopitra.Cqrs.EventStore;
using Kopitra.ManagementApi.Domain.ExpertAdvisors;
using Kopitra.ManagementApi.Infrastructure.ReadModels;
using Kopitra.ManagementApi.Time;

namespace Kopitra.ManagementApi.Application.ExpertAdvisors.Commands;

public sealed record ApproveExpertAdvisorCommand(
    string TenantId,
    string ExpertAdvisorId,
    string ApprovedBy) : ICommand<ExpertAdvisorReadModel>;

public sealed class ApproveExpertAdvisorCommandHandler : ICommandHandler<ApproveExpertAdvisorCommand, ExpertAdvisorReadModel>
{
    private readonly AggregateRepository<ExpertAdvisorAggregate, string> _repository;
    private readonly IExpertAdvisorReadModelStore _readModelStore;
    private readonly IClock _clock;

    public ApproveExpertAdvisorCommandHandler(
        AggregateRepository<ExpertAdvisorAggregate, string> repository,
        IExpertAdvisorReadModelStore readModelStore,
        IClock clock)
    {
        _repository = repository;
        _readModelStore = readModelStore;
        _clock = clock;
    }

    public async Task<ExpertAdvisorReadModel> HandleAsync(ApproveExpertAdvisorCommand command, CancellationToken cancellationToken)
    {
        var aggregate = await _repository.GetAsync(command.ExpertAdvisorId, cancellationToken).ConfigureAwait(false);
        if (!string.Equals(aggregate.TenantId, command.TenantId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Tenant mismatch for expert advisor approval.");
        }

        aggregate.Approve(command.ApprovedBy, _clock.UtcNow);
        await _repository.SaveAsync(aggregate, cancellationToken).ConfigureAwait(false);
        var readModel = await _readModelStore.GetAsync(command.TenantId, command.ExpertAdvisorId, cancellationToken).ConfigureAwait(false);
        if (readModel is null)
        {
            throw new InvalidOperationException("Expert advisor read model missing after approval.");
        }

        return readModel;
    }
}
