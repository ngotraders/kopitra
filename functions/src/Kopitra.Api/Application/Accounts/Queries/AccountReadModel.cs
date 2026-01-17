using EventFlow.Aggregates;
using EventFlow.ReadStores;
using Kopitra.Api.Domain.Accounts;
using Kopitra.Api.Domain.Accounts.Events;
using Kopitra.Api.Domain.ValueObjects;

namespace Kopitra.Api.Application.Accounts.Queries
{
    /// <summary>
    /// Read model for Account queries
    /// </summary>
    public class AccountReadModel : IReadModel,
        IAmReadModelFor<AccountAggregate, AccountId, AccountRegisteredEvent>,
        IAmReadModelFor<AccountAggregate, AccountId, AccountBalanceUpdatedEvent>,
        IAmReadModelFor<AccountAggregate, AccountId, AccountInfoUpdatedEvent>,
        IAmReadModelFor<AccountAggregate, AccountId, AccountConnectionVerifiedEvent>,
        IAmReadModelFor<AccountAggregate, AccountId, AccountDeletedEvent>
    {
        public string Id { get; set; } = null!;
        public string UserId { get; set; } = null!;
        public int BrokerType { get; set; }
        public string BrokerName { get; set; } = null!;
        public string AccountNumber { get; set; } = null!;
        public string ServerName { get; set; } = null!;
        public string? ApiKey { get; set; }
        public string? ApiSecret { get; set; }
        public decimal Balance { get; set; } = 0;
        public bool IsConnected { get; set; } = false;
        public int ConnectionStatus { get; set; } = (int)Domain.Accounts.ConnectionStatus.NotConnected;
        public DateTime? LastSyncedAt { get; set; }
        public DateTime? LastVerifiedAt { get; set; }
        public bool IsDeleted { get; set; } = false;
        public DateTime CreatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }

        public Task ApplyAsync(IReadModelContext context, IDomainEvent<AccountAggregate, AccountId, AccountRegisteredEvent> domainEvent, CancellationToken cancellationToken)
        {
            var @event = domainEvent.AggregateEvent;
            Id = context.ReadModelId;
            UserId = @event.UserId.Value;
            BrokerType = (int)@event.BrokerType;
            BrokerName = @event.BrokerName;
            AccountNumber = @event.AccountNumber;
            ServerName = @event.ServerName;
            ApiKey = @event.ApiKey;
            ApiSecret = @event.ApiSecret;
            CreatedAt = domainEvent.Timestamp.UtcDateTime;
            return Task.CompletedTask;
        }

        public Task ApplyAsync(IReadModelContext context, IDomainEvent<AccountAggregate, AccountId, AccountBalanceUpdatedEvent> domainEvent, CancellationToken cancellationToken)
        {
            var @event = domainEvent.AggregateEvent;
            Id = context.ReadModelId;
            Balance = @event.Balance;
            LastSyncedAt = @event.SyncedAt;
            return Task.CompletedTask;
        }

        public Task ApplyAsync(IReadModelContext context, IDomainEvent<AccountAggregate, AccountId, AccountInfoUpdatedEvent> domainEvent, CancellationToken cancellationToken)
        {
            var @event = domainEvent.AggregateEvent;
            Id = context.ReadModelId;
            if (!string.IsNullOrWhiteSpace(@event.AccountNumber))
                AccountNumber = @event.AccountNumber;
            if (!string.IsNullOrWhiteSpace(@event.ServerName))
                ServerName = @event.ServerName;
            if (@event.ApiKey != null)
                ApiKey = @event.ApiKey;
            if (@event.ApiSecret != null)
                ApiSecret = @event.ApiSecret;
            return Task.CompletedTask;
        }

        public Task ApplyAsync(IReadModelContext context, IDomainEvent<AccountAggregate, AccountId, AccountConnectionVerifiedEvent> domainEvent, CancellationToken cancellationToken)
        {
            var @event = domainEvent.AggregateEvent;
            Id = context.ReadModelId;
            IsConnected = @event.IsConnected;
            Balance = @event.CurrentBalance;
            LastVerifiedAt = @event.VerifiedAt;
            ConnectionStatus = @event.IsConnected ? (int)Domain.Accounts.ConnectionStatus.Connected : (int)Domain.Accounts.ConnectionStatus.NotConnected;
            return Task.CompletedTask;
        }

        public Task ApplyAsync(IReadModelContext context, IDomainEvent<AccountAggregate, AccountId, AccountDeletedEvent> domainEvent, CancellationToken cancellationToken)
        {
            Id = context.ReadModelId;
            IsDeleted = true;
            DeletedAt = domainEvent.AggregateEvent.DeletedAt;
            return Task.CompletedTask;
        }
    }
}
