using EventFlow.EntityFramework;
using EventFlow.Queries;
using Microsoft.EntityFrameworkCore;

namespace Kopitra.Api.Application.Users.Queries;

/// <summary>
/// Handler for GetUserSessionBySessionIdQuery
/// </summary>
public class GetUserSessionBySessionIdQueryHandler : IQueryHandler<GetUserSessionBySessionIdQuery, UserSessionReadModel?>
{
    private readonly IDbContextProvider<KopitraDbContext> _contextProvider;

    public GetUserSessionBySessionIdQueryHandler(IDbContextProvider<KopitraDbContext> contextProvider)
    {
        _contextProvider = contextProvider;
    }

    public async Task<UserSessionReadModel?> ExecuteQueryAsync(GetUserSessionBySessionIdQuery query, CancellationToken cancellationToken)
    {
        using var context = _contextProvider.CreateContext();
        var user = await context.UserSessions
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.SessionId == query.SessionId, cancellationToken)
            .ConfigureAwait(false);

        return user;
    }
}
