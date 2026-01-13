using EventFlow.Aggregates;
using Kopitra.Api.Domain.ValueObjects;

namespace Kopitra.Api.Domain.Accounts.Events;

/// <summary>
/// Event fired when an activation code is generated
/// </summary>
public class ActivationCodeGeneratedEvent : AggregateEvent<ActivationCodeAggregate, ActivationCodeId>
{
    public string Code { get; set; } = null!;
    public string BrokerName { get; set; } = null!;
    public string AccountNumber { get; set; } = null!;
    public string ServerName { get; set; } = null!;
    public string UserId { get; set; } = null!;
    public DateTime ExpiresAt { get; set; }
    public DateTime GeneratedAt { get; set; }
}
