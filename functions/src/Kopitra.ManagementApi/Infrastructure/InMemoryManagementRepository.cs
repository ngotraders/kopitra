using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Kopitra.ManagementApi.Accounts;
using Kopitra.ManagementApi.Automation;
using Kopitra.ManagementApi.Time;

namespace Kopitra.ManagementApi.Infrastructure;

public sealed class InMemoryManagementRepository : IManagementRepository
{
    private readonly ConcurrentDictionary<string, TenantState> _mutableTenants;

    public InMemoryManagementRepository(IClock clock)
    {
        _mutableTenants = new ConcurrentDictionary<string, TenantState>(SeedTenants(clock), StringComparer.OrdinalIgnoreCase);
    }

    public Task<IReadOnlyList<ManagedAccountSummary>> ListAccountSummariesAsync(string tenantId, CancellationToken cancellationToken)
    {
        if (!_mutableTenants.TryGetValue(tenantId, out var tenant))
        {
            return Task.FromResult<IReadOnlyList<ManagedAccountSummary>>(Array.Empty<ManagedAccountSummary>());
        }

        var summaries = tenant.GetAccountSummaries();
        return Task.FromResult<IReadOnlyList<ManagedAccountSummary>>(summaries);
    }

    public Task<ManagedAccount?> FindAccountAsync(string tenantId, string accountId, CancellationToken cancellationToken)
    {
        if (!_mutableTenants.TryGetValue(tenantId, out var tenant))
        {
            return Task.FromResult<ManagedAccount?>(null);
        }

        var account = tenant.FindAccount(accountId);
        return Task.FromResult(account);
    }

    public Task<IReadOnlyList<AutomationTask>> ListAutomationTasksAsync(string tenantId, CancellationToken cancellationToken)
    {
        if (!_mutableTenants.TryGetValue(tenantId, out var tenant))
        {
            return Task.FromResult<IReadOnlyList<AutomationTask>>(Array.Empty<AutomationTask>());
        }

        return Task.FromResult<IReadOnlyList<AutomationTask>>(tenant.GetAutomationTasks());
    }

    public Task<AutomationTask?> FindAutomationTaskAsync(string tenantId, string taskId, CancellationToken cancellationToken)
    {
        if (!_mutableTenants.TryGetValue(tenantId, out var tenant))
        {
            return Task.FromResult<AutomationTask?>(null);
        }

        var task = tenant.FindAutomationTask(taskId);
        return Task.FromResult(task);
    }

    public Task UpdateAutomationTaskExecutionAsync(string tenantId, string taskId, TaskExecutionSummary summary, CancellationToken cancellationToken)
    {
        if (_mutableTenants.TryGetValue(tenantId, out var tenant))
        {
            tenant.UpdateAutomationTask(taskId, summary);
        }

        return Task.CompletedTask;
    }

    private static IReadOnlyDictionary<string, TenantState> SeedTenants(IClock clock)
    {
        var now = clock.UtcNow;
        var tenants = new Dictionary<string, TenantState>(StringComparer.OrdinalIgnoreCase)
        {
            ["demo"] = CreateDemoTenant(now),
            ["operations"] = CreateOperationsTenant(now)
        };

        return tenants;
    }

    private static TenantState CreateDemoTenant(DateTimeOffset now)
    {
        var accounts = new List<ManagedAccount>
        {
            new(
                AccountId: "master-eurusd",
                DisplayName: "EURUSD Master",
                Broker: "OANDA",
                Platform: "MT5",
                Status: AccountStatus.Active,
                Tags: new List<string> { "forex", "tier-1" },
                Metrics: new AccountMetrics(125_000m, 3_500m, 8_200m, 24_000m, 3, 58, 2.4m),
                Risk: new AccountRiskSettings(12.5m, 65m, 15m),
                Session: new AccountSessionSnapshot(1, now.AddMinutes(-2), true, "sig-845"),
                Description: "Primary master account for premium followers.",
                Metadata: new Dictionary<string, string>
                {
                    ["region"] = "global",
                    ["strategy"] = "swing"
                },
                CreatedAt: now.AddMonths(-14),
                UpdatedAt: now.AddMinutes(-1)),
            new(
                AccountId: "asia-scalper",
                DisplayName: "Asia Session Scalper",
                Broker: "IC Markets",
                Platform: "MT4",
                Status: AccountStatus.Suspended,
                Tags: new List<string> { "forex", "asia" },
                Metrics: new AccountMetrics(54_000m, -1_200m, 1_800m, 9_600m, 1, 23, 8.7m),
                Risk: new AccountRiskSettings(8.0m, 45m, 10m),
                Session: new AccountSessionSnapshot(0, now.AddHours(-5), true, null),
                Description: "High-frequency scalper paused after exceeding drawdown guards.",
                Metadata: new Dictionary<string, string>
                {
                    ["region"] = "asia",
                    ["strategy"] = "scalping"
                },
                CreatedAt: now.AddMonths(-6),
                UpdatedAt: now.AddHours(-3))
        };

        var tasks = new List<AutomationTask>
        {
            new(
                TaskId: "daily-reconciliation",
                DisplayName: "Daily Reconciliation",
                Category: "operations",
                Description: "Rebuilds follower allocations against the latest broker statements.",
                Enabled: true,
                Schedule: new TaskSchedule(TaskScheduleType.Cron, "0 3 * * *", null, true),
                Parameters: new Dictionary<string, string>
                {
                    ["window"] = "24h",
                    ["notifyOnCompletion"] = "true"
                },
                LastExecution: new TaskExecutionSummary(
                    Status: "Succeeded",
                    StartedAt: now.AddHours(-26),
                    CompletedAt: now.AddHours(-25).AddMinutes(-45),
                    RunId: "run-20240108-0300",
                    Message: "Processed 58 follower accounts.")),
            new(
                TaskId: "refresh-auth-queue",
                DisplayName: "Refresh Pending Session Approvals",
                Category: "compliance",
                Description: "Polls Service Bus for pending EA session approvals and syncs statuses.",
                Enabled: true,
                Schedule: new TaskSchedule(TaskScheduleType.Interval, null, TimeSpan.FromMinutes(15), true),
                Parameters: new Dictionary<string, string>
                {
                    ["maxBatchSize"] = "50"
                },
                LastExecution: new TaskExecutionSummary(
                    Status: "Running",
                    StartedAt: now.AddMinutes(-5),
                    CompletedAt: null,
                    RunId: "run-" + now.AddMinutes(-5).ToString("yyyyMMddHHmm", CultureInfo.InvariantCulture),
                    Message: "Processing auth approvals."))
        };

        return new TenantState(accounts, tasks);
    }

    private static TenantState CreateOperationsTenant(DateTimeOffset now)
    {
        var accounts = new List<ManagedAccount>
        {
            new(
                AccountId: "ops-lab",
                DisplayName: "Operations Lab",
                Broker: "PaperTrade",
                Platform: "MT5",
                Status: AccountStatus.PendingApproval,
                Tags: new List<string> { "lab", "sandbox" },
                Metrics: new AccountMetrics(12_500m, 320m, 740m, 1_520m, 0, 4, 1.1m),
                Risk: new AccountRiskSettings(20m, 80m, 25m),
                Session: new AccountSessionSnapshot(0, null, false, null),
                Description: "Sandbox environment for testing automation tasks.",
                Metadata: new Dictionary<string, string>
                {
                    ["region"] = "emea",
                    ["strategy"] = "sandbox"
                },
                CreatedAt: now.AddMonths(-2),
                UpdatedAt: now.AddDays(-1))
        };

        var tasks = new List<AutomationTask>
        {
            new(
                TaskId: "seed-lab-data",
                DisplayName: "Seed Lab Followers",
                Category: "operations",
                Description: "Initializes lab tenant with synthetic follower accounts for demos.",
                Enabled: false,
                Schedule: new TaskSchedule(TaskScheduleType.Manual, null, null, true),
                Parameters: new Dictionary<string, string>
                {
                    ["followers"] = "25"
                },
                LastExecution: new TaskExecutionSummary(
                    Status: "Disabled",
                    StartedAt: null,
                    CompletedAt: null,
                    RunId: null,
                    Message: "Task disabled until lab refresh."))
        };

        return new TenantState(accounts, tasks);
    }

    private sealed class TenantState
    {
        private readonly List<ManagedAccount> _accounts;
        private readonly List<AutomationTask> _tasks;
        private readonly object _lock = new();

        public TenantState(List<ManagedAccount> accounts, List<AutomationTask> tasks)
        {
            _accounts = accounts;
            _tasks = tasks;
        }

        public IReadOnlyList<ManagedAccountSummary> GetAccountSummaries()
        {
            lock (_lock)
            {
                return _accounts
                    .Select(account => new ManagedAccountSummary(
                        account.AccountId,
                        account.DisplayName,
                        account.Broker,
                        account.Platform,
                        account.Status,
                        account.Tags.ToList(),
                        account.Metrics,
                        account.UpdatedAt))
                    .ToList();
            }
        }

        public ManagedAccount? FindAccount(string accountId)
        {
            lock (_lock)
            {
                return _accounts.FirstOrDefault(account => string.Equals(account.AccountId, accountId, StringComparison.OrdinalIgnoreCase));
            }
        }

        public IReadOnlyList<AutomationTask> GetAutomationTasks()
        {
            lock (_lock)
            {
                return _tasks.Select(task => task with { Parameters = task.Parameters.ToDictionary(kvp => kvp.Key, kvp => kvp.Value) }).ToList();
            }
        }

        public AutomationTask? FindAutomationTask(string taskId)
        {
            lock (_lock)
            {
                return _tasks.FirstOrDefault(task => string.Equals(task.TaskId, taskId, StringComparison.OrdinalIgnoreCase));
            }
        }

        public void UpdateAutomationTask(string taskId, TaskExecutionSummary summary)
        {
            lock (_lock)
            {
                var index = _tasks.FindIndex(task => string.Equals(task.TaskId, taskId, StringComparison.OrdinalIgnoreCase));
                if (index < 0)
                {
                    return;
                }

                var task = _tasks[index];
                _tasks[index] = task with { LastExecution = summary };
            }
        }
    }
}
