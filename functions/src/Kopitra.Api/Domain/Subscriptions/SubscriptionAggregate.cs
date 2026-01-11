using EventFlow.Aggregates;
using Kopitra.Api.Domain.Subscriptions.Events;
using Kopitra.Api.Domain.ValueObjects;

namespace Kopitra.Api.Domain.Subscriptions;

/// <summary>
/// Subscription aggregate - represents subscription of copy trading from provider
/// </summary>
public class SubscriptionAggregate : AggregateRoot<SubscriptionAggregate, SubscriptionId>
{
    public UserId SubscriberId { get; private set; } = null!;
    public ProviderId ProviderId { get; private set; } = null!;
    public AccountId SubscriberAccountId { get; private set; } = null!;
    public FundingStrategy FundingStrategy { get; private set; }
    public decimal? MaxEquityRisk { get; private set; }
    public SubscriptionStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? CanceledAt { get; private set; }

    public SubscriptionAggregate(SubscriptionId id) : base(id)
    {
    }

    /// <summary>
    /// Create new subscription
    /// </summary>
    public void Create(
        UserId subscriberId,
        ProviderId providerId,
        AccountId subscriberAccountId,
        FundingStrategy fundingStrategy,
        decimal? maxEquityRisk = null)
    {
        if (subscriberId == null)
            throw new ArgumentNullException(nameof(subscriberId));
        if (providerId == null)
            throw new ArgumentNullException(nameof(providerId));
        if (subscriberAccountId == null)
            throw new ArgumentNullException(nameof(subscriberAccountId));
        if (maxEquityRisk.HasValue && (maxEquityRisk <= 0 || maxEquityRisk > 100))
            throw new ArgumentException("Max equity risk must be between 0 and 100.", nameof(maxEquityRisk));

        Emit(new SubscriptionCreatedEvent
        {
            SubscriberId = subscriberId,
            ProviderId = providerId,
            SubscriberAccountId = subscriberAccountId,
            FundingStrategy = fundingStrategy,
            MaxEquityRisk = maxEquityRisk,
            CreatedAt = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Pause subscription temporarily
    /// </summary>
    public void Pause()
    {
        if (Status != SubscriptionStatus.Active)
            throw new InvalidOperationException("Only active subscriptions can be paused.");

        Emit(new SubscriptionPausedEvent { PausedAt = DateTime.UtcNow });
    }

    /// <summary>
    /// Resume paused subscription
    /// </summary>
    public void Resume()
    {
        if (Status != SubscriptionStatus.Paused)
            throw new InvalidOperationException("Only paused subscriptions can be resumed.");

        Emit(new SubscriptionResumedEvent { ResumedAt = DateTime.UtcNow });
    }

    /// <summary>
    /// Cancel subscription
    /// </summary>
    public void Cancel(string reason = "User canceled subscription")
    {
        if (Status == SubscriptionStatus.Canceled)
            throw new InvalidOperationException("Subscription is already canceled.");

        Emit(new SubscriptionCanceledEvent
        {
            Reason = reason,
            CanceledAt = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Update subscription settings
    /// </summary>
    public void UpdateSettings(
        FundingStrategy? newFundingStrategy = null,
        decimal? newMaxEquityRisk = null)
    {
        if (Status == SubscriptionStatus.Canceled)
            throw new InvalidOperationException("Cannot update canceled subscription.");
        if (newFundingStrategy == null && newMaxEquityRisk == null)
            throw new ArgumentException("At least one parameter must be provided for update.");
        if (newMaxEquityRisk.HasValue && (newMaxEquityRisk <= 0 || newMaxEquityRisk > 100))
            throw new ArgumentException("Max equity risk must be between 0 and 100.", nameof(newMaxEquityRisk));

        Emit(new SubscriptionSettingsUpdatedEvent
        {
            NewFundingStrategy = newFundingStrategy,
            NewMaxEquityRisk = newMaxEquityRisk,
            UpdatedAt = DateTime.UtcNow
        });
    }

    private void Apply(SubscriptionCreatedEvent domainEvent)
    {
        SubscriberId = domainEvent.SubscriberId;
        ProviderId = domainEvent.ProviderId;
        SubscriberAccountId = domainEvent.SubscriberAccountId;
        FundingStrategy = domainEvent.FundingStrategy;
        MaxEquityRisk = domainEvent.MaxEquityRisk;
        Status = SubscriptionStatus.Active;
        CreatedAt = domainEvent.CreatedAt;
    }

    private void Apply(SubscriptionPausedEvent domainEvent)
    {
        Status = SubscriptionStatus.Paused;
    }

    private void Apply(SubscriptionResumedEvent domainEvent)
    {
        Status = SubscriptionStatus.Active;
    }

    private void Apply(SubscriptionCanceledEvent domainEvent)
    {
        Status = SubscriptionStatus.Canceled;
        CanceledAt = domainEvent.CanceledAt;
    }

    private void Apply(SubscriptionSettingsUpdatedEvent domainEvent)
    {
        if (domainEvent.NewFundingStrategy.HasValue)
            FundingStrategy = domainEvent.NewFundingStrategy.Value;
        if (domainEvent.NewMaxEquityRisk.HasValue)
            MaxEquityRisk = domainEvent.NewMaxEquityRisk.Value;
    }
}
