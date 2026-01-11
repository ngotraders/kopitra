using EventFlow.Core;

namespace Kopitra.Api.Domain.ValueObjects;

/// <summary>
/// Subscription aggregate ID
/// </summary>
public class SubscriptionId : Identity<SubscriptionId>
{
    public SubscriptionId(string value) : base(value)
    {
    }
}
