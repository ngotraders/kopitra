using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Kopitra.ManagementApi.Infrastructure.ReadModels;

public interface IExpertAdvisorReadModelStore
{
    Task UpsertAsync(ExpertAdvisorReadModel model, CancellationToken cancellationToken);

    Task<ExpertAdvisorReadModel?> GetAsync(string tenantId, string expertAdvisorId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<ExpertAdvisorReadModel>> ListAsync(string tenantId, CancellationToken cancellationToken);
}
