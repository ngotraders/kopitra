using EventFlow.Commands;
using Kopitra.Api.Domain.Accounts;
using Kopitra.Api.Domain.ValueObjects;

namespace Kopitra.Api.Application.Accounts.Commands;

public class ConfirmActivationCodeCommand : Command<ActivationCodeAggregate, ActivationCodeId>
{
    public BrokerType BrokerType { get; set; }

    public ConfirmActivationCodeCommand(ActivationCodeId activationCodeId) : base(activationCodeId) { }
}
