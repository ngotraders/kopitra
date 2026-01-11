using EventFlow.Commands;
using Kopitra.Api.Domain.Users;
using Kopitra.Api.Domain.ValueObjects;

namespace Kopitra.Api.Application.Users.Commands;

public class EnableProviderCommand : Command<UserAggregate, UserId>
{
    public string ProviderDescription { get; set; } = null!;

    public EnableProviderCommand(UserId aggregateId) : base(aggregateId) { }
}
