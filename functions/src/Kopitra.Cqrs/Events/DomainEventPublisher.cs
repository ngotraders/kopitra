using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Kopitra.Cqrs.Events;

public sealed class DomainEventPublisher : IDomainEventPublisher
{
    private readonly IServiceProvider _serviceProvider;

    public DomainEventPublisher(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task PublishAsync(IEnumerable<IDomainEventEnvelope> events, CancellationToken cancellationToken)
    {
        foreach (var envelope in events)
        {
            var eventType = envelope.Event.GetType();
            var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(eventType);
            var handlersType = typeof(IEnumerable<>).MakeGenericType(handlerType);
            var handlersObj = _serviceProvider.GetService(handlersType) as IEnumerable ?? Array.Empty<object>();
            var envelopeType = typeof(DomainEventEnvelope<>).MakeGenericType(eventType);
            var typedEnvelope = Convert.ChangeType(envelope, envelopeType);

            foreach (var handler in handlersObj)
            {
                var method = handlerType.GetMethod("HandleAsync");
                if (method == null)
                {
                    continue;
                }

                var task = (Task?)method.Invoke(handler, new[] { typedEnvelope!, cancellationToken });
                if (task is not null)
                {
                    await task.ConfigureAwait(false);
                }
            }
        }
    }
}
