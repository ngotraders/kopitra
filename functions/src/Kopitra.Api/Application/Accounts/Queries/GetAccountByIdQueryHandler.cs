using EventFlow.Queries;
using Kopitra.Api.Domain.Accounts.ValueObjects;
using Kopitra.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kopitra.Api.Application.Accounts.Queries
{
    public class GetAccountByIdQueryHandler : IQueryHandler<GetAccountByIdQuery, AccountReadModel?>
    {
        private readonly IDbContextProvider<KopitraDbContext> _contextProvider;

        public GetAccountByIdQueryHandler(IDbContextProvider<KopitraDbContext> contextProvider)
        {
            _contextProvider = contextProvider ?? throw new ArgumentNullException(nameof(contextProvider));
        }

        public async Task<AccountReadModel?> ExecuteQueryAsync(GetAccountByIdQuery query, CancellationToken cancellationToken)
        {
            using var context = _contextProvider.CreateContext();
            var account = await context.Accounts
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == query.AccountId.Value && !a.IsDeleted, cancellationToken)
                .ConfigureAwait(false);

            return account;
        }
    }
}
