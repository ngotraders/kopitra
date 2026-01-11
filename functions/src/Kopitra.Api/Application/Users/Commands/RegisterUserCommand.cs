using EventFlow.Commands;
using Kopitra.Api.Domain.Users;
using Kopitra.Api.Domain.ValueObjects;

namespace Kopitra.Api.Application.Users.Commands
{
    public class RegisterUserCommand : Command<UserAggregate, UserId>
    {
        public string Email { get; set; } = null!;
        public string DisplayName { get; set; } = null!;
        public string PasswordHash { get; set; } = null!;

        public RegisterUserCommand(UserId aggregateId) : base(aggregateId) { }
    }
}
