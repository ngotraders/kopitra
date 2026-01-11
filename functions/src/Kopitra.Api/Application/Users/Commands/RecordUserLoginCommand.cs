using EventFlow.Commands;
using Kopitra.Api.Domain.Users;
using Kopitra.Api.Domain.ValueObjects;

namespace Kopitra.Api.Application.Users.Commands;

public class RecordUserLoginCommand : Command<UserAggregate, UserId>
{
    public RecordUserLoginCommand(UserId aggregateId) : base(aggregateId) { }
}
