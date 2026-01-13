using EventFlow.Aggregates;
using Kopitra.Api.Domain.Accounts.Events;
using Kopitra.Api.Domain.ValueObjects;

namespace Kopitra.Api.Domain.Accounts;

/// <summary>
/// Connection status for trading account
/// </summary>
public enum ConnectionStatus
{
    NotConnected,
    TestPending,
    Connected
}

/// <summary>
/// Account aggregate - represents a single trading account (MT4/MT5) for a user
/// </summary>
public class AccountAggregate : AggregateRoot<AccountAggregate, AccountId>
{
    public UserId UserId { get; private set; } = null!;
    public BrokerType BrokerType { get; private set; }
    public string BrokerName { get; private set; } = null!;
    public string AccountNumber { get; private set; } = null!;
    public string ServerName { get; private set; } = null!;
    public string? ApiKey { get; private set; }
    public string? ApiSecret { get; private set; }
    public decimal Balance { get; private set; } = 0;
    public bool IsConnected { get; private set; } = false;
    public ConnectionStatus ConnectionStatus { get; private set; } = ConnectionStatus.NotConnected;
    public DateTime? LastSyncedAt { get; private set; }
    public DateTime? LastVerifiedAt { get; private set; }
    public bool IsDeleted { get; private set; } = false;

    public AccountAggregate(AccountId id) : base(id)
    {
    }

    /// <summary>
    /// Register a new trading account (Flow 1: Pre-configured)
    /// </summary>
    public void Register(UserId userId, BrokerType brokerType, string brokerName, 
        string accountNumber, string serverName, string? apiKey = null, string? apiSecret = null)
    {
        if (userId == null)
            throw new ArgumentNullException(nameof(userId));
        if (string.IsNullOrWhiteSpace(brokerName))
            throw new ArgumentException("Broker name is required.", nameof(brokerName));
        if (string.IsNullOrWhiteSpace(accountNumber))
            throw new ArgumentException("Account number is required.", nameof(accountNumber));
        if (string.IsNullOrWhiteSpace(serverName))
            throw new ArgumentException("Server name is required.", nameof(serverName));

        Emit(new AccountRegisteredEvent
        {
            UserId = userId,
            BrokerType = brokerType,
            BrokerName = brokerName,
            AccountNumber = accountNumber,
            ServerName = serverName,
            ApiKey = apiKey,
            ApiSecret = apiSecret,
        });
    }

    /// <summary>
    /// Verify account connection (typically called after connection test)
    /// </summary>
    public void VerifyConnection(bool isConnected, decimal currentBalance)
    {
        if (IsDeleted)
            throw new InvalidOperationException("Cannot verify a deleted account.");

        Emit(new AccountConnectionVerifiedEvent
        {
            IsConnected = isConnected,
            CurrentBalance = currentBalance,
            VerifiedAt = DateTime.UtcNow,
        });
    }

    /// <summary>
    /// Initiate account activation (Flow 2: EA-initiated)
    /// Triggered when EA sends broker info for the first time
    /// </summary>
    public void InitiateActivation(string brokerName, string accountNumber, string serverName)
    {
        if (string.IsNullOrWhiteSpace(brokerName))
            throw new ArgumentException("Broker name is required.", nameof(brokerName));
        if (string.IsNullOrWhiteSpace(accountNumber))
            throw new ArgumentException("Account number is required.", nameof(accountNumber));
        if (string.IsNullOrWhiteSpace(serverName))
            throw new ArgumentException("Server name is required.", nameof(serverName));

        Emit(new AccountActivationInitiatedEvent
        {
            BrokerName = brokerName,
            AccountNumber = accountNumber,
            ServerName = serverName,
            InitiatedAt = DateTime.UtcNow,
        });
    }

    /// <summary>
    /// Confirm account activation after user confirms activation code or enters account info manually
    /// </summary>
    public void ConfirmActivation(UserId userId, BrokerType brokerType)
    {
        if (userId == null)
            throw new ArgumentNullException(nameof(userId));
        if (IsDeleted)
            throw new InvalidOperationException("Cannot confirm activation on a deleted account.");

        Emit(new AccountActivationConfirmedEvent
        {
            UserId = userId,
            BrokerType = brokerType,
            ConfirmedAt = DateTime.UtcNow,
        });
    }

    /// <summary>
    /// Update account balance (called from EA sync)
    /// </summary>
    public void UpdateBalance(decimal newBalance)
    {
        if (IsDeleted)
            throw new InvalidOperationException("Cannot update balance on a deleted account.");
        if (newBalance < 0)
            throw new ArgumentException("Balance cannot be negative.", nameof(newBalance));

        Emit(new AccountBalanceUpdatedEvent
        {
            Balance = newBalance,
            SyncedAt = DateTime.UtcNow,
        });
    }

    /// <summary>
    /// Update account information
    /// </summary>
    public void UpdateInfo(string? accountNumber = null, string? serverName = null, 
        string? apiKey = null, string? apiSecret = null)
    {
        if (IsDeleted)
            throw new InvalidOperationException("Cannot update a deleted account.");

        // At least one field should be provided
        if (string.IsNullOrWhiteSpace(accountNumber) && string.IsNullOrWhiteSpace(serverName) &&
            string.IsNullOrWhiteSpace(apiKey) && string.IsNullOrWhiteSpace(apiSecret))
        {
            throw new ArgumentException("At least one field must be provided.", nameof(accountNumber));
        }

        Emit(new AccountInfoUpdatedEvent
        {
            AccountNumber = accountNumber,
            ServerName = serverName,
            ApiKey = apiKey,
            ApiSecret = apiSecret,
        });
    }

    /// <summary>
    /// Delete this account
    /// </summary>
    public void Delete()
    {
        if (IsDeleted)
            throw new InvalidOperationException("Account is already deleted.");

        Emit(new AccountDeletedEvent
        {
            DeletedAt = DateTime.UtcNow,
        });
    }

    // Event application
    private void Apply(AccountRegisteredEvent @event)
    {
        UserId = @event.UserId;
        BrokerType = @event.BrokerType;
        BrokerName = @event.BrokerName;
        AccountNumber = @event.AccountNumber;
        ServerName = @event.ServerName;
        ApiKey = @event.ApiKey;
        ApiSecret = @event.ApiSecret;
        IsConnected = false;
        ConnectionStatus = ConnectionStatus.TestPending;
        Balance = 0;
    }

    private void Apply(AccountActivationInitiatedEvent @event)
    {
        BrokerName = @event.BrokerName;
        AccountNumber = @event.AccountNumber;
        ServerName = @event.ServerName;
        ConnectionStatus = ConnectionStatus.NotConnected;
    }

    private void Apply(AccountActivationConfirmedEvent @event)
    {
        UserId = @event.UserId;
        BrokerType = @event.BrokerType;
        ConnectionStatus = ConnectionStatus.TestPending;
    }

    private void Apply(AccountConnectionVerifiedEvent @event)
    {
        IsConnected = @event.IsConnected;
        Balance = @event.CurrentBalance;
        LastVerifiedAt = @event.VerifiedAt;
        ConnectionStatus = @event.IsConnected ? ConnectionStatus.Connected : ConnectionStatus.NotConnected;
    }

    private void Apply(AccountBalanceUpdatedEvent @event)
    {
        Balance = @event.Balance;
        LastSyncedAt = @event.SyncedAt;
    }

    private void Apply(AccountInfoUpdatedEvent @event)
    {
        if (!string.IsNullOrWhiteSpace(@event.AccountNumber))
            AccountNumber = @event.AccountNumber;
        if (!string.IsNullOrWhiteSpace(@event.ServerName))
            ServerName = @event.ServerName;
        if (!string.IsNullOrWhiteSpace(@event.ApiKey))
            ApiKey = @event.ApiKey;
        if (!string.IsNullOrWhiteSpace(@event.ApiSecret))
            ApiSecret = @event.ApiSecret;
    }

    private void Apply(AccountDeletedEvent @event)
    {
        IsDeleted = true;
    }
}
