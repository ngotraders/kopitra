using EventFlow.Aggregates;
using Kopitra.Api.Domain.ValueObjects;

namespace Kopitra.Api.Domain.Users.Events;

/// <summary>
/// Admin impersonation started event - fired when admin acts as user
/// </summary>
public class UserAdminImpersonationStartedEvent : AggregateEvent<UserAggregate, UserId>
{
    public UserId AdminUserId { get; set; } = null!;
    public string Action { get; set; } = null!;
    public Dictionary<string, object> Details { get; set; } = new();
}