using EventFlow.Queries;
using Kopitra.Api.Domain.Accounts;

namespace Kopitra.Api.Application.Accounts.Queries;

public class GetActivationCodeByCodeQueryHandler : IQueryHandler<GetActivationCodeByCodeQuery, ActivationCodeReadModel>
{
    public Task<ActivationCodeReadModel> ExecuteQueryAsync(GetActivationCodeByCodeQuery query, CancellationToken cancellationToken)
    {
        throw new NotImplementedException("This query handler needs to be implemented with database access");
    }
}
