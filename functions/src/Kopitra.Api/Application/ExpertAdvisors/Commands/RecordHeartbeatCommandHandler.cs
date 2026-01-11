using EventFlow.Commands;
using Kopitra.Api.Domain.ExpertAdvisors;
using Kopitra.Api.Common;

namespace Kopitra.Api.Application.ExpertAdvisors.Commands;

public class RecordHeartbeatCommandHandler : CommandHandler<ExpertAdvisorSessionAggregate, ExpertAdvisorSessionId, RecordHeartbeatCommand>
{
    private readonly IClock _clock;

    public RecordHeartbeatCommandHandler(IClock clock)
    {
        _clock = clock;
    }

    public override Task ExecuteAsync(ExpertAdvisorSessionAggregate aggregate, RecordHeartbeatCommand command, CancellationToken cancellationToken)
    {
        aggregate.RecordHeartbeat(_clock.UtcNow);
        return Task.CompletedTask;
    }
}
