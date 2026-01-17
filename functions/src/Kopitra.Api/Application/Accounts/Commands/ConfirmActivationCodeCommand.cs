using EventFlow.Commands;
using Kopitra.Api.Common;
using Kopitra.Api.Domain.Accounts;
using Kopitra.Api.Domain.ValueObjects;

namespace Kopitra.Api.Application.Accounts.Commands;

public class ConfirmActivationCodeCommand : Command<ActivationCodeAggregate, ActivationCodeId>
{
    public UserId UserId { get; set; } = null!;

    public ConfirmActivationCodeCommand(ActivationCodeId activationCodeId) : base(activationCodeId) { }
}
