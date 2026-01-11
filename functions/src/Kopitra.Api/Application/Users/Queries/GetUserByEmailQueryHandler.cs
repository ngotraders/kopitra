using EventFlow.EntityFramework;
using EventFlow.Queries;
using Microsoft.EntityFrameworkCore;

namespace Kopitra.Api.Application.Users.Queries;

public class GetUserByEmailQueryHandler : IQueryHandler<GetUserByEmailQuery, UserReadModel>
{
    private readonly IDbContextProvider<KopitraDbContext> _contextProvider;

    public GetUserByEmailQueryHandler(IDbContextProvider<KopitraDbContext> contextProvider)
    {
        _contextProvider = contextProvider;
    }

    public async Task<UserReadModel?> ExecuteQueryAsync(GetUserByEmailQuery query, CancellationToken cancellationToken)
    {
        using var context = _contextProvider.CreateContext();
        var user = await context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == query.Email, cancellationToken).ConfigureAwait(false);
        if (user == null) throw new InvalidOperationException("User not found");
        return user;
    }
}
