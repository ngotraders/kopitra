using EventFlow.EntityFramework;
using EventFlow.Queries;
using Kopitra.Api.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Kopitra.Api.Application.Accounts.Queries
{
    public class GetAccountByIdQuery : IQuery<AccountReadModel?>
    {
        public AccountId AccountId { get; }

        public GetAccountByIdQuery(AccountId accountId)
        {
            AccountId = accountId ?? throw new ArgumentNullException(nameof(accountId));
        }
    }
}
