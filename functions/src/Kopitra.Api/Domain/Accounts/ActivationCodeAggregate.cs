using EventFlow.Aggregates;
using Kopitra.Api.Domain.Accounts.Events;
using Kopitra.Api.Domain.ValueObjects;

namespace Kopitra.Api.Domain.Accounts;

/// <summary>
/// ActivationCode aggregate - manages the lifecycle of activation codes used in Flow 2
/// </summary>
public class ActivationCodeAggregate : AggregateRoot<ActivationCodeAggregate, ActivationCodeId>
{
    public string Code { get; private set; } = null!;
    public string BrokerName { get; private set; } = null!;
    public string AccountNumber { get; private set; } = null!;
    public string ServerName { get; private set; } = null!;
    public string UserId { get; private set; } = null!;
    public ActivationCodeStatus Status { get; private set; } = ActivationCodeStatus.Pending;
    public DateTime ExpiresAt { get; private set; }
    public DateTime? ConfirmedAt { get; private set; }
    public DateTime GeneratedAt { get; private set; }

    public ActivationCodeAggregate(ActivationCodeId id) : base(id)
    {
    }

    /// <summary>
    /// Generate a new activation code
    /// </summary>
    public void Generate(string code, string brokerName, string accountNumber, string serverName, 
        string userId, DateTime expiresAt)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Code is required.", nameof(code));
        if (string.IsNullOrWhiteSpace(brokerName))
            throw new ArgumentException("Broker name is required.", nameof(brokerName));
        if (string.IsNullOrWhiteSpace(accountNumber))
            throw new ArgumentException("Account number is required.", nameof(accountNumber));
        if (string.IsNullOrWhiteSpace(serverName))
            throw new ArgumentException("Server name is required.", nameof(serverName));
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("User ID is required.", nameof(userId));
        if (expiresAt <= DateTime.UtcNow)
            throw new ArgumentException("Expiration time must be in the future.", nameof(expiresAt));

        Emit(new ActivationCodeGeneratedEvent
        {
            Code = code,
            BrokerName = brokerName,
            AccountNumber = accountNumber,
            ServerName = serverName,
            UserId = userId,
            ExpiresAt = expiresAt,
            GeneratedAt = DateTime.UtcNow,
        });
    }

    /// <summary>
    /// Confirm/redeem the activation code
    /// </summary>
    public void Confirm(BrokerType brokerType)
    {
        if (Status != ActivationCodeStatus.Pending)
            throw new InvalidOperationException("Can only confirm a pending activation code.");
        
        if (DateTime.UtcNow > ExpiresAt)
            throw new InvalidOperationException("Activation code has expired.");

        Emit(new ActivationCodeConfirmedEvent
        {
            UserId = UserId,
            BrokerType = brokerType,
            ConfirmedAt = DateTime.UtcNow,
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

    /// <summary>
    /// Check if the code is still valid
    /// </summary>
    public bool IsValid()
    {
        return Status == ActivationCodeStatus.Pending && DateTime.UtcNow <= ExpiresAt;
    }

    // Event application
    private void Apply(ActivationCodeGeneratedEvent @event)
    {
        Code = @event.Code;
        BrokerName = @event.BrokerName;
        AccountNumber = @event.AccountNumber;
        ServerName = @event.ServerName;
        UserId = @event.UserId;
        ExpiresAt = @event.ExpiresAt;
        GeneratedAt = @event.GeneratedAt;
        Status = ActivationCodeStatus.Pending;
    }

    private void Apply(ActivationCodeConfirmedEvent @event)
    {
        ConfirmedAt = @event.ConfirmedAt;
        Status = ActivationCodeStatus.Confirmed;
    }

    private void Apply(ActivationCodeExpiredEvent @event)
    {
        Status = ActivationCodeStatus.Expired;
    }
}
