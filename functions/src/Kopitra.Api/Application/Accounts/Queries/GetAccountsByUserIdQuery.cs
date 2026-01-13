using EventFlow.EntityFramework;
using EventFlow.Queries;
using Kopitra.Api.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Kopitra.Api.Application.Accounts.Queries
{
    public class GetAccountsByUserIdQuery : IQuery<List<AccountReadModel>>
    {
        public UserId UserId { get; }

        public GetAccountsByUserIdQuery(UserId userId)
        {
            UserId = userId ?? throw new ArgumentNullException(nameof(userId));
        }
    }
}
