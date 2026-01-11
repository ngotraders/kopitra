using EventFlow.Commands;
using Kopitra.Api.Domain.ExpertAdvisors;
using Kopitra.Api.Common;

namespace Kopitra.Api.Application.ExpertAdvisors.Commands;

public class CreateSessionCommandHandler : CommandHandler<ExpertAdvisorSessionAggregate, ExpertAdvisorSessionId, CreateSessionCommand>
{
    private readonly IClock _clock;

    public CreateSessionCommandHandler(IClock clock)
    {
        _clock = clock;
    }

    public override Task ExecuteAsync(ExpertAdvisorSessionAggregate aggregate, CreateSessionCommand command, CancellationToken cancellationToken)
    {
        aggregate.Create(command.UserId, command.AccountId, _clock.UtcNow);
        return Task.CompletedTask;
    }
}
