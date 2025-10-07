using System.Threading;
using System.Threading.Tasks;

namespace Kopitra.ManagementApi.Infrastructure.AdminUsers;

public interface IAdminUserCredentialStore
{
    Task SetAsync(AdminUserCredential credential, CancellationToken cancellationToken);

    Task<AdminUserCredential?> GetAsync(string tenantId, string email, CancellationToken cancellationToken);
}
