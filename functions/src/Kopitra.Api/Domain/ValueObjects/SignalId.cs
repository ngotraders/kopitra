using EventFlow.Core;

namespace Kopitra.Api.Domain.ValueObjects;

/// <summary>
/// Signal aggregate ID
/// </summary>
public class SignalId : Identity<SignalId>
{
    public SignalId(string value) : base(value)
    {
    }
}
