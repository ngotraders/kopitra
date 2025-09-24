using Kopitra.ManagementApi.Accounts;
using Kopitra.ManagementApi.Automation;

namespace Kopitra.ManagementApi.Infrastructure;

public interface IManagementRepository
{
    Task<IReadOnlyList<ManagedAccountSummary>> ListAccountSummariesAsync(string tenantId, CancellationToken cancellationToken);

    Task<ManagedAccount?> FindAccountAsync(string tenantId, string accountId, CancellationToken cancellationToken);

    Task<IReadOnlyList<AutomationTask>> ListAutomationTasksAsync(string tenantId, CancellationToken cancellationToken);

    Task<AutomationTask?> FindAutomationTaskAsync(string tenantId, string taskId, CancellationToken cancellationToken);

    Task UpdateAutomationTaskExecutionAsync(string tenantId, string taskId, TaskExecutionSummary summary, CancellationToken cancellationToken);
}
