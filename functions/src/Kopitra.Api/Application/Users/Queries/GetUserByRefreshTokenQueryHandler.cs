using EventFlow.EntityFramework;
using EventFlow.Queries;
using Microsoft.EntityFrameworkCore;

namespace Kopitra.Api.Application.Users.Queries;

/// <summary>
/// Handler for GetUserByIdQuery
/// </summary>
public class GetUserByRefreshTokenQueryHandler : IQueryHandler<GetUserByRefreshTokenQuery, UserReadModel>
{
    private readonly IDbContextProvider<KopitraDbContext> _contextProvider;

    public GetUserByRefreshTokenQueryHandler(IDbContextProvider<KopitraDbContext> contextProvider)
    {
        _contextProvider = contextProvider;
    }

    public async Task<UserReadModel> ExecuteQueryAsync(GetUserByRefreshTokenQuery query, CancellationToken cancellationToken)
    {
        using var context = _contextProvider.CreateContext();
        var user = await context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.RefreshToken == query.RefreshToken, cancellationToken)
            .ConfigureAwait(false);

        if (user == null) throw new InvalidOperationException("User not found");

        return user;
    }
}
