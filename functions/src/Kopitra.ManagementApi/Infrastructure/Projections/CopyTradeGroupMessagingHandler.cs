using System.Threading;
using System.Threading.Tasks;
using EventFlow.Aggregates;
using EventFlow.Subscribers;
using Kopitra.ManagementApi.Domain.CopyTrading;
using Kopitra.ManagementApi.Infrastructure.Messaging;

namespace Kopitra.ManagementApi.Infrastructure.Projections;

public sealed class CopyTradeGroupMessagingHandler :
    ISubscribeSynchronousTo<CopyTradeGroupAggregate, CopyTradeGroupId, CopyTradeGroupMemberUpserted>,
    ISubscribeSynchronousTo<CopyTradeGroupAggregate, CopyTradeGroupId, CopyTradeGroupMemberRemoved>
{
    private readonly IServiceBusPublisher _publisher;

    public CopyTradeGroupMessagingHandler(IServiceBusPublisher publisher)
    {
        _publisher = publisher;
    }

    public Task HandleAsync(IDomainEvent<CopyTradeGroupAggregate, CopyTradeGroupId, CopyTradeGroupMemberUpserted> domainEvent, CancellationToken cancellationToken)
    {
        var payload = new
        {
            domainEvent.AggregateEvent.TenantId,
            domainEvent.AggregateEvent.GroupId,
            domainEvent.AggregateEvent.MemberId,
            domainEvent.AggregateEvent.Role,
            domainEvent.AggregateEvent.RiskStrategy,
            domainEvent.AggregateEvent.Allocation,
            domainEvent.AggregateEvent.UpdatedBy,
            domainEvent.AggregateEvent.UpdatedAt,
            Type = "copy-trade-member-upserted"
        };
        return _publisher.PublishAsync("copy-trade-members", payload, cancellationToken);
    }

    public Task HandleAsync(IDomainEvent<CopyTradeGroupAggregate, CopyTradeGroupId, CopyTradeGroupMemberRemoved> domainEvent, CancellationToken cancellationToken)
    {
        var payload = new
        {
            domainEvent.AggregateEvent.TenantId,
            domainEvent.AggregateEvent.GroupId,
            domainEvent.AggregateEvent.MemberId,
            domainEvent.AggregateEvent.RemovedBy,
            domainEvent.AggregateEvent.RemovedAt,
            Type = "copy-trade-member-removed"
        };
        return _publisher.PublishAsync("copy-trade-members", payload, cancellationToken);
    }
}
