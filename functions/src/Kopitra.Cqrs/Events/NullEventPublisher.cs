namespace Kopitra.Cqrs.Events;

public sealed class NullEventPublisher : IEventPublisher
{
    public Task PublishAsync(EventEnvelope envelope, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
