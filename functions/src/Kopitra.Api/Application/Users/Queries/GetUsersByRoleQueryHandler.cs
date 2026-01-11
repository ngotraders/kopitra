using EventFlow.EntityFramework;
using EventFlow.Queries;
using Microsoft.EntityFrameworkCore;

namespace Kopitra.Api.Application.Users.Queries;

/// <summary>
/// Handler for GetUsersByRoleQuery
/// </summary>
public class GetUsersByRoleQueryHandler : IQueryHandler<GetUsersByRoleQuery, IEnumerable<UserReadModel>>
{
    private readonly IDbContextProvider<KopitraDbContext> _contextProvider;

    public GetUsersByRoleQueryHandler(IDbContextProvider<KopitraDbContext> contextProvider)
    {
        _contextProvider = contextProvider;
    }

    public async Task<IEnumerable<UserReadModel>> ExecuteQueryAsync(GetUsersByRoleQuery query, CancellationToken cancellationToken)
    {
        using var context = _contextProvider.CreateContext();

        // Filter users based on role
        IQueryable<UserReadModel> usersQuery = query.Role.ToLower() switch
        {
            "provider" => context.Users.Where(u => u.CanProvide),
            "subscriber" => context.Users.Where(u => u.CanSubscribe),
            "admin" => context.Users.Where(u => u.Roles.Contains("Admin")),
            _ => context.Users
        };

        var users = await usersQuery
            .AsNoTracking()
            .OrderByDescending(u => u.RegisteredAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return users;
    }
}
