using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Kopitra.Cqrs.Queries;
using Kopitra.ManagementApi.Infrastructure.ReadModels;

namespace Kopitra.ManagementApi.Application.AdminUsers.Queries;

public sealed record ListAdminUsersQuery(string TenantId) : IQuery<IReadOnlyCollection<AdminUserReadModel>>;

public sealed class ListAdminUsersQueryHandler : IQueryHandler<ListAdminUsersQuery, IReadOnlyCollection<AdminUserReadModel>>
{
    private readonly IAdminUserReadModelStore _store;

    public ListAdminUsersQueryHandler(IAdminUserReadModelStore store)
    {
        _store = store;
    }

    public Task<IReadOnlyCollection<AdminUserReadModel>> HandleAsync(ListAdminUsersQuery query, CancellationToken cancellationToken)
    {
        return _store.ListAsync(query.TenantId, cancellationToken);
    }
}
