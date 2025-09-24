using Kopitra.ManagementApi.Automation;
using Kopitra.ManagementApi.Infrastructure;
using Kopitra.ManagementApi.Tests;
using Xunit;

namespace Kopitra.ManagementApi.Tests.Automation;

public class AutomationTaskServiceTests
{
    private readonly TestClock _clock = new(new DateTimeOffset(2024, 1, 12, 7, 30, 0, TimeSpan.Zero));

    [Fact]
    public async Task RunTaskAsync_UpdatesRepositoryAndReturnsRunDetails()
    {
        var repository = new InMemoryManagementRepository(_clock);
        var service = new AutomationTaskService(repository, _clock);

        var response = await service.RunTaskAsync("demo", "daily-reconciliation", CancellationToken.None);

        Assert.Equal("daily-reconciliation", response.TaskId);
        Assert.Equal("Accepted", response.Status);
        Assert.Equal(_clock.UtcNow, response.SubmittedAt);
        Assert.False(string.IsNullOrWhiteSpace(response.RunId));

        var updated = await repository.FindAutomationTaskAsync("demo", "daily-reconciliation", CancellationToken.None);
        Assert.NotNull(updated);
        Assert.Equal("Pending", updated!.LastExecution.Status);
        Assert.Equal(response.RunId, updated.LastExecution.RunId);
    }

    [Fact]
    public async Task RunTaskAsync_WhenTaskIsMissing_ThrowsNotFound()
    {
        var repository = new InMemoryManagementRepository(_clock);
        var service = new AutomationTaskService(repository, _clock);

        await Assert.ThrowsAsync<AutomationTaskNotFoundException>(() => service.RunTaskAsync("demo", "missing-task", CancellationToken.None));
    }
}
