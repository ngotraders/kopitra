using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace Kopitra.ManagementApi.Infrastructure.Authentication;

public interface IAccessTokenValidator
{
    Task<ClaimsPrincipal> ValidateAsync(string token, CancellationToken cancellationToken);
}
