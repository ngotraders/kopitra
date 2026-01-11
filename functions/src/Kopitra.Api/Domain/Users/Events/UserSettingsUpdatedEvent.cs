using EventFlow.Aggregates;
using Kopitra.Api.Domain.ValueObjects;
using System.Collections.Generic;

namespace Kopitra.Api.Domain.Users.Events;

/// <summary>
/// User settings updated event
/// </summary>
public class UserSettingsUpdatedEvent : AggregateEvent<UserAggregate, UserId>
{
    public Dictionary<string, object> Settings { get; set; } = new();
}
