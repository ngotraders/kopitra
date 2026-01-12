using EventFlow.EntityFramework;
using EventFlow.Queries;
using Microsoft.EntityFrameworkCore;

namespace Kopitra.Api.Application.Users.Queries;

/// <summary>
/// Handler for GetUserSessionByRefreshTokenQuery
/// </summary>
public class GetUserSessionByRefreshTokenQueryHandler : IQueryHandler<GetUserSessionByRefreshTokenQuery, UserSessionReadModel?>
{
    private readonly IDbContextProvider<KopitraDbContext> _contextProvider;

    public GetUserSessionByRefreshTokenQueryHandler(IDbContextProvider<KopitraDbContext> contextProvider)
    {
        _contextProvider = contextProvider;
    }

    public async Task<UserSessionReadModel?> ExecuteQueryAsync(GetUserSessionByRefreshTokenQuery query, CancellationToken cancellationToken)
    {
        using var context = _contextProvider.CreateContext();
        var user = await context.UserSessions
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.RefreshToken == query.RefreshToken, cancellationToken)
            .ConfigureAwait(false);

        return user;
    }
}
