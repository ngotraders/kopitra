using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Kopitra.ManagementApi.Infrastructure.ReadModels;

public interface IAdminUserReadModelStore
{
    Task UpsertAsync(AdminUserReadModel model, CancellationToken cancellationToken);

    Task<AdminUserReadModel?> GetAsync(string tenantId, string userId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<AdminUserReadModel>> ListAsync(string tenantId, CancellationToken cancellationToken);
}
