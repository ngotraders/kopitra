using EventFlow.Commands;
using Kopitra.Api.Domain.ExpertAdvisors;
using Kopitra.Api.Common;

namespace Kopitra.Api.Application.ExpertAdvisors.Commands;

public class CloseSessionCommandHandler : CommandHandler<ExpertAdvisorSessionAggregate, ExpertAdvisorSessionId, CloseSessionCommand>
{
    private readonly IClock _clock;

    public CloseSessionCommandHandler(IClock clock)
    {
        _clock = clock;
    }

    public override Task ExecuteAsync(ExpertAdvisorSessionAggregate aggregate, CloseSessionCommand command, CancellationToken cancellationToken)
    {
        aggregate.Close(_clock.UtcNow);
        return Task.CompletedTask;
    }
}
