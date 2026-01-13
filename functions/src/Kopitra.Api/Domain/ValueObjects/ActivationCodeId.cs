using EventFlow.Core;

namespace Kopitra.Api.Domain.ValueObjects;

/// <summary>
/// Value object representing an activation code ID
/// </summary>
public class ActivationCodeId : Identity<ActivationCodeId>
{
    public ActivationCodeId(string value) : base(value)
    {
    }
}
