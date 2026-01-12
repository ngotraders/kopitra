using EventFlow.Queries;

namespace Kopitra.Api.Application.Users.Queries;

public class GetUserSessionByRefreshTokenQuery : IQuery<UserSessionReadModel?>
{
    public string RefreshToken { get; }

    public GetUserSessionByRefreshTokenQuery(string refreshToken)
    {
        RefreshToken = refreshToken;
    }
}
