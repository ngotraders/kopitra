using EventFlow.Queries;

namespace Kopitra.Api.Application.Users.Queries;

public class GetUserByRefreshTokenQuery : IQuery<UserReadModel>
{
    public string RefreshToken { get; }

    public GetUserByRefreshTokenQuery(string refreshToken)
    {
        RefreshToken = refreshToken;
    }
}
