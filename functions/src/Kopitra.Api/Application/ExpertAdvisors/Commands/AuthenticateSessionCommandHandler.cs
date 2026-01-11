using System;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Commands;
using Kopitra.Api.Domain.ExpertAdvisors;
using Kopitra.Api.Application.Users.Services;

namespace Kopitra.Api.Application.ExpertAdvisors.Commands;

public class AuthenticateSessionCommandHandler : CommandHandler<ExpertAdvisorSessionAggregate, ExpertAdvisorSessionId, AuthenticateSessionCommand>
{
    private readonly ITokenService _tokenService;

    public AuthenticateSessionCommandHandler(ITokenService tokenService)
    {
        _tokenService = tokenService;
    }

    public override Task ExecuteAsync(ExpertAdvisorSessionAggregate aggregate, AuthenticateSessionCommand command, CancellationToken cancellationToken)
    {
        var subject = string.IsNullOrEmpty(aggregate.UserId) ? aggregate.Id.Value : aggregate.UserId;
        var token = _tokenService.GenerateToken(subject);
        aggregate.Authenticate(DateTimeOffset.UtcNow, token);
        return Task.CompletedTask;
    }
}
