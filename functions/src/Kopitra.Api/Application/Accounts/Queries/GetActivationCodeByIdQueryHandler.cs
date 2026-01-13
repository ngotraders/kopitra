using EventFlow.Queries;
using Kopitra.Api.Domain.ValueObjects;

namespace Kopitra.Api.Application.Accounts.Queries;

public class GetActivationCodeByIdQueryHandler : IQueryHandler<GetActivationCodeByIdQuery, ActivationCodeReadModel>
{
    public Task<ActivationCodeReadModel> ExecuteQueryAsync(GetActivationCodeByIdQuery query, CancellationToken cancellationToken)
    {
        throw new NotImplementedException("This query handler needs to be implemented with database access");
    }
}
