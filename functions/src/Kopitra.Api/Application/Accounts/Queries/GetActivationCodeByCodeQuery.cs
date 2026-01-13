using EventFlow.Queries;
using Kopitra.Api.Domain.Accounts;

namespace Kopitra.Api.Application.Accounts.Queries;

/// <summary>
/// Query to get an activation code by code value
/// </summary>
public class GetActivationCodeByCodeQuery : IQuery<ActivationCodeReadModel>
{
    public string Code { get; }

    public GetActivationCodeByCodeQuery(string code)
    {
        Code = code;
    }
}
