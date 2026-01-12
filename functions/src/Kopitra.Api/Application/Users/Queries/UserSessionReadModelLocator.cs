using EventFlow.Aggregates;
using EventFlow.ReadStores;
using Kopitra.Api.Domain.Users;
using Kopitra.Api.Domain.Users.Events;
using Kopitra.Api.Domain.ValueObjects;

namespace Kopitra.Api.Application.Users.Queries;

public class UserSessionReadModelLocator : IReadModelLocator
{
    public IEnumerable<string> GetReadModelIds(IDomainEvent domainEvent)
    {
        var sessionId = domainEvent switch
        {
            IDomainEvent<UserAggregate, UserId, UserRefreshTokenIssuedEvent> e => e.AggregateEvent.SessionId,
            IDomainEvent<UserAggregate, UserId, UserRefreshTokenRevokedEvent> e => e.AggregateEvent.SessionId,
            _ => null,
        };
        if (string.IsNullOrEmpty(sessionId))
        {
            yield break;
        }
        yield return sessionId;
    }
}
