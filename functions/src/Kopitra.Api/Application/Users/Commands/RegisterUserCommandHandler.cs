using EventFlow.Commands;
using Kopitra.Api.Domain.Users;
using Kopitra.Api.Domain.ValueObjects;

namespace Kopitra.Api.Application.Users.Commands
{
    public class RegisterUserCommandHandler : CommandHandler<UserAggregate, UserId, RegisterUserCommand>
    {
        public override Task ExecuteAsync(UserAggregate aggregate, RegisterUserCommand command, CancellationToken cancellationToken)
        {
            aggregate.Register(command.Email, command.DisplayName, command.PasswordHash);
            return Task.CompletedTask;
        }
    }
}
