using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Kopitra.ManagementApi.Common.Cqrs;
using Kopitra.ManagementApi.Domain.Integration;
using Kopitra.ManagementApi.Infrastructure.EventLog;

namespace Kopitra.ManagementApi.Application.Integration.Queries;

public sealed record ListEaIntegrationEventsQuery(string TenantId) : IQuery<IReadOnlyCollection<EaIntegrationEvent>>;

public sealed class ListEaIntegrationEventsQueryHandler : IQueryHandler<ListEaIntegrationEventsQuery, IReadOnlyCollection<EaIntegrationEvent>>
{
    private readonly IEaIntegrationEventStore _store;

    public ListEaIntegrationEventsQueryHandler(IEaIntegrationEventStore store)
    {
        _store = store;
    }

    public Task<IReadOnlyCollection<EaIntegrationEvent>> HandleAsync(ListEaIntegrationEventsQuery query, CancellationToken cancellationToken)
    {
        return _store.ListAsync(query.TenantId, cancellationToken);
    }
}
