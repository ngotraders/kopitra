using EventFlow.Commands;
using Kopitra.Api.Application.Users.Services;
using Kopitra.Api.Common;
using Kopitra.Api.Domain.ExpertAdvisors;

namespace Kopitra.Api.Application.ExpertAdvisors.Commands;

public class AuthenticateSessionCommandHandler : CommandHandler<ExpertAdvisorSessionAggregate, ExpertAdvisorSessionId, AuthenticateSessionCommand>
{
    private readonly ITokenService _tokenService;
    private readonly IClock _clock;

    public AuthenticateSessionCommandHandler(ITokenService tokenService, IClock clock)
    {
        _tokenService = tokenService;
        _clock = clock;
    }

    public override Task ExecuteAsync(ExpertAdvisorSessionAggregate aggregate, AuthenticateSessionCommand command, CancellationToken cancellationToken)
    {
        var subject = string.IsNullOrEmpty(aggregate.UserId) ? aggregate.Id.Value : aggregate.UserId;
        var token = _tokenService.GenerateToken(subject);
        aggregate.Authenticate(_clock.UtcNow, token);
        return Task.CompletedTask;
    }
}
