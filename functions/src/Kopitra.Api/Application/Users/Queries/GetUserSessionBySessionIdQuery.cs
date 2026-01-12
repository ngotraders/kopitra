using EventFlow.Queries;

namespace Kopitra.Api.Application.Users.Queries;

public class GetUserSessionBySessionIdQuery : IQuery<UserSessionReadModel?>
{
    public string SessionId { get; }

    public GetUserSessionBySessionIdQuery(string sessionId)
    {
        SessionId = sessionId;
    }
}
