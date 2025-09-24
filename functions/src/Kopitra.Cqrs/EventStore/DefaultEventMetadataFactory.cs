using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Kopitra.Cqrs.Events;

namespace Kopitra.Cqrs.EventStore;

public sealed class DefaultEventMetadataFactory : IEventMetadataFactory
{
    public IReadOnlyDictionary<string, string> Create(IDomainEvent domainEvent)
    {
        if (domainEvent is null)
        {
            throw new ArgumentNullException(nameof(domainEvent));
        }

        var data = new Dictionary<string, string>
        {
            ["eventType"] = domainEvent.GetType().FullName ?? domainEvent.GetType().Name
        };

        return new ReadOnlyDictionary<string, string>(data);
    }
}
