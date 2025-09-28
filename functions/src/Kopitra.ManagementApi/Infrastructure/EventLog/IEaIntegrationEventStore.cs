using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Kopitra.ManagementApi.Domain.Integration;

namespace Kopitra.ManagementApi.Infrastructure.EventLog;

public interface IEaIntegrationEventStore
{
    Task AppendAsync(EaIntegrationEvent integrationEvent, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<EaIntegrationEvent>> ListAsync(string tenantId, CancellationToken cancellationToken);
}
