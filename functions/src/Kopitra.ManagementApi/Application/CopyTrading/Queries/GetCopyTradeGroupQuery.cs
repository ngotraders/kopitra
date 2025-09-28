using System.Threading;
using System.Threading.Tasks;
using Kopitra.Cqrs.Queries;
using Kopitra.ManagementApi.Infrastructure.ReadModels;

namespace Kopitra.ManagementApi.Application.CopyTrading.Queries;

public sealed record GetCopyTradeGroupQuery(string TenantId, string GroupId) : IQuery<CopyTradeGroupReadModel?>;

public sealed class GetCopyTradeGroupQueryHandler : IQueryHandler<GetCopyTradeGroupQuery, CopyTradeGroupReadModel?>
{
    private readonly ICopyTradeGroupReadModelStore _store;

    public GetCopyTradeGroupQueryHandler(ICopyTradeGroupReadModelStore store)
    {
        _store = store;
    }

    public Task<CopyTradeGroupReadModel?> HandleAsync(GetCopyTradeGroupQuery query, CancellationToken cancellationToken)
    {
        return _store.GetAsync(query.TenantId, query.GroupId, cancellationToken);
    }
}
