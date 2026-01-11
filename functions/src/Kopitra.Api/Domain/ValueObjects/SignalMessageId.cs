using EventFlow.Core;

namespace Kopitra.Api.Domain.ValueObjects;

/// <summary>
/// Signal message ID - used for EA polling
/// </summary>
public class SignalMessageId : Identity<SignalMessageId>
{
    public SignalMessageId(string value) : base(value)
    {
    }
}
