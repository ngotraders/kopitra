using System.Threading;
using System.Threading.Tasks;

namespace Kopitra.ManagementApi.Infrastructure.Idempotency;

public interface IIdempotencyStore
{
    Task<IdempotencyResult> TryStoreAsync(string tenantId, string key, string hash, CancellationToken cancellationToken);
}

public sealed record IdempotencyResult(bool IsNew);
