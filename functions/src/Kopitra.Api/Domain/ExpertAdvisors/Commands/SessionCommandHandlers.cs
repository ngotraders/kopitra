using EventFlow.Commands;
using Kopitra.Api.Domain.ExpertAdvisors.Events;
using Kopitra.Api.Common;

namespace Kopitra.Api.Domain.ExpertAdvisors.Commands;

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

public class AuthenticateSessionCommandHandler : CommandHandler<ExpertAdvisorSessionAggregate, ExpertAdvisorSessionId, AuthenticateSessionCommand>
{
    private readonly IClock _clock;

    public AuthenticateSessionCommandHandler(IClock clock)
    {
        _clock = clock;
    }

    public override Task ExecuteAsync(ExpertAdvisorSessionAggregate aggregate, AuthenticateSessionCommand command, CancellationToken cancellationToken)
    {
        aggregate.Authenticate(_clock.UtcNow);
        return Task.CompletedTask;
    }
}

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
