using EventFlow.Queries;

namespace Kopitra.Api.Application.Users.Queries;

public class GetUserByEmailQuery : IQuery<UserReadModel?>
{
    public string Email { get; }

    public GetUserByEmailQuery(string email)
    {
        Email = email;
    }
}
