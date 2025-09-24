using Kopitra.ManagementApi.Automation;
using Kopitra.ManagementApi.Infrastructure;
using Kopitra.ManagementApi.Tests;
using Xunit;

namespace Kopitra.ManagementApi.Tests.Infrastructure;

public class InMemoryManagementRepositoryTests
{
    private readonly TestClock _clock = new(new DateTimeOffset(2024, 1, 12, 7, 30, 0, TimeSpan.Zero));

    [Fact]
    public async Task ListAccountSummariesAsync_ReturnsTenantSpecificAccounts()
    {
        var repository = new InMemoryManagementRepository(_clock);

        var demoAccounts = await repository.ListAccountSummariesAsync("demo", CancellationToken.None);
        var opsAccounts = await repository.ListAccountSummariesAsync("operations", CancellationToken.None);
        var unknownAccounts = await repository.ListAccountSummariesAsync("missing", CancellationToken.None);

        Assert.Equal(2, demoAccounts.Count);
        Assert.Contains(demoAccounts, account => account.AccountId == "master-eurusd");
        Assert.Single(opsAccounts);
        Assert.Empty(unknownAccounts);
    }

    [Fact]
    public async Task FindAccountAsync_ReturnsDetailedAccount()
    {
        var repository = new InMemoryManagementRepository(_clock);

        var account = await repository.FindAccountAsync("demo", "asia-scalper", CancellationToken.None);

        Assert.NotNull(account);
        Assert.Equal("Asia Session Scalper", account!.DisplayName);
        Assert.Equal("scalping", account.Metadata["strategy"]);
    }

    [Fact]
    public async Task UpdateAutomationTaskExecutionAsync_PersistsLatestSummary()
    {
        var repository = new InMemoryManagementRepository(_clock);
        var initial = await repository.FindAutomationTaskAsync("demo", "daily-reconciliation", CancellationToken.None);
        Assert.NotNull(initial);

        var newSummary = new TaskExecutionSummary("Queued", _clock.UtcNow, null, "run-test", "Queued for processing");

        await repository.UpdateAutomationTaskExecutionAsync("demo", "daily-reconciliation", newSummary, CancellationToken.None);

        var updated = await repository.FindAutomationTaskAsync("demo", "daily-reconciliation", CancellationToken.None);
        Assert.NotNull(updated);
        Assert.Equal("Queued", updated!.LastExecution.Status);
        Assert.Equal("run-test", updated.LastExecution.RunId);
    }
}
