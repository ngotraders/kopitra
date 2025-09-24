using System.Collections.Generic;
using Kopitra.Cqrs.Events;

namespace Kopitra.Cqrs.EventStore;

public interface IEventMetadataFactory
{
    IReadOnlyDictionary<string, string> Create(IDomainEvent domainEvent);
}
