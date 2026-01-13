using EventFlow.Queries;
using Kopitra.Api.Domain.ValueObjects;

namespace Kopitra.Api.Application.Accounts.Queries;

/// <summary>
/// Query to get an activation code by ID
/// </summary>
public class GetActivationCodeByIdQuery : IQuery<ActivationCodeReadModel>
{
    public ActivationCodeId ActivationCodeId { get; }

    public GetActivationCodeByIdQuery(ActivationCodeId activationCodeId)
    {
        ActivationCodeId = activationCodeId;
    }
}
