using EventFlow.Aggregates;
using Kopitra.Api.Domain.ValueObjects;

namespace Kopitra.Api.Domain.Subscriptions.Events;

/// <summary>
/// Subscription status
/// </summary>
public enum SubscriptionStatus
{
    Active = 0,
    Paused = 1,
    Canceled = 2
}

/// <summary>
/// Subscription created event
/// </summary>
public class SubscriptionCreatedEvent : AggregateEvent<SubscriptionAggregate, SubscriptionId>
{
    public UserId SubscriberId { get; set; } = null!;
    public ProviderId ProviderId { get; set; } = null!;
    public AccountId SubscriberAccountId { get; set; } = null!;
    public FundingStrategy FundingStrategy { get; set; }
    public decimal? MaxEquityRisk { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Subscription paused event
/// </summary>
public class SubscriptionPausedEvent : AggregateEvent<SubscriptionAggregate, SubscriptionId>
{
    public DateTime PausedAt { get; set; }
}

/// <summary>
/// Subscription resumed event
/// </summary>
public class SubscriptionResumedEvent : AggregateEvent<SubscriptionAggregate, SubscriptionId>
{
    public DateTime ResumedAt { get; set; }
}

/// <summary>
/// Subscription canceled event
/// </summary>
public class SubscriptionCanceledEvent : AggregateEvent<SubscriptionAggregate, SubscriptionId>
{
    public string Reason { get; set; } = null!;
    public DateTime CanceledAt { get; set; }
}

/// <summary>
/// Subscription settings updated event
/// </summary>
public class SubscriptionSettingsUpdatedEvent : AggregateEvent<SubscriptionAggregate, SubscriptionId>
{
    public FundingStrategy? NewFundingStrategy { get; set; }
    public decimal? NewMaxEquityRisk { get; set; }
    public DateTime UpdatedAt { get; set; }
}
