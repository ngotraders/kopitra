using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Kopitra.ManagementApi.Infrastructure.Messaging;

public sealed class InMemoryServiceBusPublisher : IServiceBusPublisher
{
    private readonly ConcurrentQueue<(string Topic, object Payload)> _messages = new();

    public Task PublishAsync(string topicName, object payload, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _messages.Enqueue((topicName, payload));
        return Task.CompletedTask;
    }

    public IReadOnlyCollection<(string Topic, object Payload)> DequeueAll()
    {
        var list = new List<(string Topic, object Payload)>();
        while (_messages.TryDequeue(out var item))
        {
            list.Add(item);
        }

        return list;
    }
}
