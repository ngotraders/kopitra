using EventFlow.Aggregates;
using EventFlow.ReadStores;
using Kopitra.Api.Domain.Users;
using Kopitra.Api.Domain.Users.Events;
using Kopitra.Api.Domain.ValueObjects;

namespace Kopitra.Api.Application.Users.Queries;

public class UserSessionReadModel : IReadModel,
    IAmReadModelFor<UserAggregate, UserId, UserRefreshTokenIssuedEvent>,
    IAmReadModelFor<UserAggregate, UserId, UserRefreshTokenRevokedEvent>
{
    public string Id { get; set; } = null!;
    public string UserId { get; set; } = null!;
    public string SessionId { get; set; } = null!;
    public string RefreshToken { get; set; } = null!;
    public DateTimeOffset? RefreshTokenExpiresAt { get; set; }

    public Task ApplyAsync(IReadModelContext context, IDomainEvent<UserAggregate, UserId, UserRefreshTokenIssuedEvent> domainEvent, CancellationToken cancellationToken)
    {
        Id = context.ReadModelId;
        var e = domainEvent.AggregateEvent;
        UserId = domainEvent.GetIdentity().Value;
        SessionId = e.SessionId;
        RefreshToken = e.RefreshToken;
        RefreshTokenExpiresAt = domainEvent.Timestamp;
        return Task.CompletedTask;
    }

    public Task ApplyAsync(IReadModelContext context, IDomainEvent<UserAggregate, UserId, UserRefreshTokenRevokedEvent> domainEvent, CancellationToken cancellationToken)
    {
        Id = context.ReadModelId;
        context.MarkForDeletion();
        return Task.CompletedTask;
    }
}
