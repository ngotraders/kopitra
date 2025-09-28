using System.Threading;
using System.Threading.Tasks;
using Kopitra.Cqrs.Commands;
using Kopitra.Cqrs.EventStore;
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
    private readonly AggregateRepository<ExpertAdvisorAggregate, string> _repository;
    private readonly IExpertAdvisorReadModelStore _readModelStore;
    private readonly IClock _clock;

    public RegisterExpertAdvisorCommandHandler(
        AggregateRepository<ExpertAdvisorAggregate, string> repository,
        IExpertAdvisorReadModelStore readModelStore,
        IClock clock)
    {
        _repository = repository;
        _readModelStore = readModelStore;
        _clock = clock;
    }

    public async Task<ExpertAdvisorReadModel> HandleAsync(RegisterExpertAdvisorCommand command, CancellationToken cancellationToken)
    {
        var aggregate = await _repository.GetAsync(command.ExpertAdvisorId, cancellationToken).ConfigureAwait(false);
        aggregate.Register(command.TenantId, command.ExpertAdvisorId, command.DisplayName, command.Description, command.RequestedBy, _clock.UtcNow);
        await _repository.SaveAsync(aggregate, cancellationToken).ConfigureAwait(false);
        var readModel = await _readModelStore.GetAsync(command.TenantId, command.ExpertAdvisorId, cancellationToken).ConfigureAwait(false);
        return readModel ?? new ExpertAdvisorReadModel(command.TenantId, command.ExpertAdvisorId, command.DisplayName, command.Description, aggregate.Status, aggregate.ApprovedBy, _clock.UtcNow);
    }
}
