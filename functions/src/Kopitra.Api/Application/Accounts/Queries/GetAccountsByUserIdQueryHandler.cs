using EventFlow.EntityFramework;
using EventFlow.Queries;
using Microsoft.EntityFrameworkCore;

namespace Kopitra.Api.Application.Accounts.Queries
{
    public class GetAccountsByUserIdQueryHandler : IQueryHandler<GetAccountsByUserIdQuery, List<AccountReadModel>>
    {
        private readonly IDbContextProvider<KopitraDbContext> _contextProvider;

        public GetAccountsByUserIdQueryHandler(IDbContextProvider<KopitraDbContext> contextProvider)
        {
            _contextProvider = contextProvider ?? throw new ArgumentNullException(nameof(contextProvider));
        }

        public async Task<List<AccountReadModel>> ExecuteQueryAsync(GetAccountsByUserIdQuery query, CancellationToken cancellationToken)
        {
            using var context = _contextProvider.CreateContext();
            var accounts = await context.Accounts
                .AsNoTracking()
                .Where(a => a.UserId == query.UserId.Value && !a.IsDeleted)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            return accounts;
        }
    }
}
