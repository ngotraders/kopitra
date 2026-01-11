using EventFlow.Aggregates;
using Kopitra.Api.Domain.ValueObjects;

namespace Kopitra.Api.Domain.Users.Events;

/// <summary>
/// User permissions changed event - fired when roles are changed
/// </summary>
public class UserPermissionsChangedEvent : AggregateEvent<UserAggregate, UserId>
{
    public bool? CanProvide { get; set; }
    public bool? CanSubscribe { get; set; }
}
