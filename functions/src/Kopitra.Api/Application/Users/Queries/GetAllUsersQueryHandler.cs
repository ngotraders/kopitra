using EventFlow.EntityFramework;
using EventFlow.Queries;
using Microsoft.EntityFrameworkCore;

namespace Kopitra.Api.Application.Users.Queries;

/// <summary>
/// Handler for GetAllUsersQuery
/// </summary>
public class GetAllUsersQueryHandler : IQueryHandler<GetAllUsersQuery, IEnumerable<UserReadModel>>
{
    private readonly IDbContextProvider<KopitraDbContext> _contextProvider;

    public GetAllUsersQueryHandler(IDbContextProvider<KopitraDbContext> contextProvider)
    {
        _contextProvider = contextProvider;
    }

    public async Task<IEnumerable<UserReadModel>> ExecuteQueryAsync(GetAllUsersQuery query, CancellationToken cancellationToken)
    {
        using var context = _contextProvider.CreateContext();
        var users = await context.Users
            .AsNoTracking()
            .OrderByDescending(u => u.RegisteredAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return users;
    }
}
