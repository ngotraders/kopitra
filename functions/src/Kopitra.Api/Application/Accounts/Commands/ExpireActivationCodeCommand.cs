using EventFlow.Commands;
using Kopitra.Api.Domain.Accounts;
using Kopitra.Api.Domain.ValueObjects;

namespace Kopitra.Api.Application.Accounts.Commands;

public class ExpireActivationCodeCommand : Command<ActivationCodeAggregate, ActivationCodeId>
{
    public ExpireActivationCodeCommand(ActivationCodeId activationCodeId) : base(activationCodeId) { }
}
