using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Kopitra.ManagementApi.Infrastructure.ReadModels;

public interface ICopyTradeGroupReadModelStore
{
    Task UpsertAsync(CopyTradeGroupReadModel model, CancellationToken cancellationToken);

    Task<CopyTradeGroupReadModel?> GetAsync(string tenantId, string groupId, CancellationToken cancellationToken);
}
