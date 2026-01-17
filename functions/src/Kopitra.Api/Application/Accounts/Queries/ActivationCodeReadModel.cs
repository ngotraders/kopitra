using EventFlow.Aggregates;
using EventFlow.ReadStores;
using Kopitra.Api.Domain.Accounts;
using Kopitra.Api.Domain.Accounts.Events;

namespace Kopitra.Api.Application.Accounts.Queries;

/// <summary>
/// Read model for ActivationCode queries
/// </summary>
public class ActivationCodeReadModel : IReadModel,
    IAmReadModelFor<ActivationCodeAggregate, ActivationCodeId, ActivationCodeGeneratedEvent>,
    IAmReadModelFor<ActivationCodeAggregate, ActivationCodeId, ActivationCodeConfirmedEvent>,
    IAmReadModelFor<ActivationCodeAggregate, ActivationCodeId, ActivationCodeExpiredEvent>
{
    public string Id { get; set; } = null!;
    public string Code { get; set; } = null!;
    public string BrokerName { get; set; } = null!;
    public string AccountNumber { get; set; } = null!;
    public string ServerName { get; set; } = null!;
    public string UserId { get; set; } = null!;
    public int Status { get; set; } = 0; // Pending = 0, Confirmed = 1, Expired = 2
    public DateTime ExpiresAt { get; set; }
    public DateTime GeneratedAt { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public DateTime CreatedAt { get; set; }

    public Task ApplyAsync(IReadModelContext context, IDomainEvent<ActivationCodeAggregate, ActivationCodeId, ActivationCodeGeneratedEvent> domainEvent, CancellationToken cancellationToken)
    {
        var @event = domainEvent.AggregateEvent;
        Id = context.ReadModelId;
        Code = @event.Code;
        BrokerName = @event.BrokerName;
        AccountNumber = @event.AccountNumber;
        ServerName = @event.ServerName;
        UserId = @event.UserId;
        ExpiresAt = @event.ExpiresAt;
        GeneratedAt = @event.GeneratedAt;
        CreatedAt = domainEvent.Timestamp.DateTime;
        Status = 0; // Pending
        return Task.CompletedTask;
    }

    public Task ApplyAsync(IReadModelContext context, IDomainEvent<ActivationCodeAggregate, ActivationCodeId, ActivationCodeConfirmedEvent> domainEvent, CancellationToken cancellationToken)
    {
        var @event = domainEvent.AggregateEvent;
        Id = context.ReadModelId;
        Status = 1; // Confirmed
        ConfirmedAt = @event.ConfirmedAt;
        return Task.CompletedTask;
    }

    public Task ApplyAsync(IReadModelContext context, IDomainEvent<ActivationCodeAggregate, ActivationCodeId, ActivationCodeExpiredEvent> domainEvent, CancellationToken cancellationToken)
    {
        Id = context.ReadModelId;
        Status = 2; // Expired
        return Task.CompletedTask;
    }
}
