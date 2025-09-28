using System.Threading;
using System.Threading.Tasks;
using Kopitra.Cqrs.Events;
using Kopitra.ManagementApi.Domain.CopyTrading;
using Kopitra.ManagementApi.Infrastructure.Messaging;

namespace Kopitra.ManagementApi.Infrastructure.Projections;

public sealed class CopyTradeGroupMessagingHandler :
    IDomainEventHandler<CopyTradeGroupMemberUpserted>,
    IDomainEventHandler<CopyTradeGroupMemberRemoved>
{
    private readonly IServiceBusPublisher _publisher;

    public CopyTradeGroupMessagingHandler(IServiceBusPublisher publisher)
    {
        _publisher = publisher;
    }

    public Task HandleAsync(DomainEventEnvelope<CopyTradeGroupMemberUpserted> envelope, CancellationToken cancellationToken)
    {
        var payload = new
        {
            envelope.Event.TenantId,
            envelope.Event.GroupId,
            envelope.Event.MemberId,
            envelope.Event.Role,
            envelope.Event.RiskStrategy,
            envelope.Event.Allocation,
            envelope.Event.UpdatedBy,
            envelope.Event.UpdatedAt,
            Type = "copy-trade-member-upserted"
        };
        return _publisher.PublishAsync("copy-trade-members", payload, cancellationToken);
    }

    public Task HandleAsync(DomainEventEnvelope<CopyTradeGroupMemberRemoved> envelope, CancellationToken cancellationToken)
    {
        var payload = new
        {
            envelope.Event.TenantId,
            envelope.Event.GroupId,
            envelope.Event.MemberId,
            envelope.Event.RemovedBy,
            envelope.Event.RemovedAt,
            Type = "copy-trade-member-removed"
        };
        return _publisher.PublishAsync("copy-trade-members", payload, cancellationToken);
    }
}
