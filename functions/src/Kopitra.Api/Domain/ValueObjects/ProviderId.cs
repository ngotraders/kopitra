using EventFlow.Core;

namespace Kopitra.Api.Domain.ValueObjects;

/// <summary>
/// Provider aggregate ID (uses same as UserId with provider designation)
/// </summary>
public class ProviderId : Identity<ProviderId>
{
    public ProviderId(string value) : base(value)
    {
    }
}
