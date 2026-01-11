using EventFlow.EntityFramework;
using EventFlow.Queries;
using Microsoft.EntityFrameworkCore;

namespace Kopitra.Api.Application.Users.Queries;

/// <summary>
/// Handler for GetUserByIdQuery
/// </summary>
public class GetUserByIdQueryHandler : IQueryHandler<GetUserByIdQuery, UserReadModel>
{
    private readonly IDbContextProvider<KopitraDbContext> _contextProvider;

    public GetUserByIdQueryHandler(IDbContextProvider<KopitraDbContext> contextProvider)
    {
        _contextProvider = contextProvider;
    }

    public async Task<UserReadModel> ExecuteQueryAsync(GetUserByIdQuery query, CancellationToken cancellationToken)
    {
        using var context = _contextProvider.CreateContext();
        var user = await context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == query.UserId.Value, cancellationToken)
            .ConfigureAwait(false);

        if (user == null)
            throw new InvalidOperationException($"User not found: {query.UserId.Value}");

        return user;
    }
}
