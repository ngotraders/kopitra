using EventFlow.Aggregates;
using Kopitra.Api.Domain.Accounts.Events;
using Kopitra.Api.Domain.ValueObjects;

namespace Kopitra.Api.Domain.Accounts;

/// <summary>
/// ActivationCode aggregate - manages the lifecycle of activation codes used in Flow 2
/// </summary>
public class ActivationCodeAggregate : AggregateRoot<ActivationCodeAggregate, ActivationCodeId>,
    IApply<ActivationCodeGeneratedEvent>,
    IApply<ActivationCodeConfirmedEvent>
    , IApply<ActivationCodeExpiredEvent>
{
    public string Code { get; private set; } = null!;
    public BrokerType BrokerType { get; private set; }
    public string BrokerName { get; private set; } = null!;
    public string AccountNumber { get; private set; } = null!;
    public string ServerName { get; private set; } = null!;
    public UserId? UserId { get; private set; }
    public ActivationCodeStatus Status { get; private set; } = ActivationCodeStatus.Pending;
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset? ConfirmedAt { get; private set; }

    public ActivationCodeAggregate(ActivationCodeId id) : base(id)
    {
    }

    /// <summary>
    /// Generate a new activation code
    /// </summary>
    public void Generate(
        string code, 
        BrokerType brokerType, 
        string brokerName, 
        string accountNumber, 
        string serverName, 
        UserId? userId,
        DateTimeOffset expiresAt)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Code is required.", nameof(code));
        if (brokerType != BrokerType.MT4 && brokerType != BrokerType.MT5)
            throw new ArgumentException("Broker type is required.", nameof(brokerType));
        if (string.IsNullOrWhiteSpace(brokerName))
            throw new ArgumentException("Broker name is required.", nameof(brokerName));
        if (string.IsNullOrWhiteSpace(accountNumber))
            throw new ArgumentException("Account number is required.", nameof(accountNumber));
        if (string.IsNullOrWhiteSpace(serverName))
            throw new ArgumentException("Server name is required.", nameof(serverName));

        Emit(new ActivationCodeGeneratedEvent
        {
            Code = code,
            BrokerType = brokerType,
            BrokerName = brokerName,
            AccountNumber = accountNumber,
            ServerName = serverName,
            UserId = userId,
            ExpiresAt = expiresAt,
        });
    }

    /// <summary>
    /// Confirm/redeem the activation code
    /// </summary>
    public void Confirm(UserId userId, DateTimeOffset confirmedAt)
    {
        if (UserId != null && userId != UserId)
            throw new InvalidOperationException("Can only confirm a pending activation code.");

        if (Status != ActivationCodeStatus.Pending)
            throw new InvalidOperationException("Can only confirm a pending activation code.");
        
        if (confirmedAt > ExpiresAt)
            throw new InvalidOperationException("Activation code has expired.");

        Emit(new ActivationCodeConfirmedEvent
        {
            UserId = userId,
            ConfirmedAt = confirmedAt,
        });
    }

    /// <summary>
    /// Mark the activation code as expired
    /// </summary>
    public void Expire()
    {
        if (Status == ActivationCodeStatus.Expired)
            throw new InvalidOperationException("Code is already expired.");
        
        if (Status == ActivationCodeStatus.Confirmed)
            throw new InvalidOperationException("Cannot expire a confirmed code.");

        Emit(new ActivationCodeExpiredEvent
        {
            ExpiredAt = DateTime.UtcNow,
        });
    }

    // Event application
    public void Apply(ActivationCodeGeneratedEvent @event)
    {
        Code = @event.Code;
        BrokerType = @event.BrokerType;
        BrokerName = @event.BrokerName;
        AccountNumber = @event.AccountNumber;
        ServerName = @event.ServerName;
        UserId = @event.UserId;
        ExpiresAt = @event.ExpiresAt;
        Status = ActivationCodeStatus.Pending;
    }

    public void Apply(ActivationCodeConfirmedEvent @event)
    {
        UserId = @event.UserId;
        ConfirmedAt = @event.ConfirmedAt;
        Status = ActivationCodeStatus.Confirmed;
    }

    public void Apply(ActivationCodeExpiredEvent @event)
    {
        Status = ActivationCodeStatus.Expired;
    }
}
