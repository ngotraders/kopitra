using Kopitra.ManagementApi.Diagnostics;
using Kopitra.ManagementApi.Tests;
using Xunit;

namespace Kopitra.ManagementApi.Tests.Diagnostics;

public class HealthReporterTests
{
    [Fact]
    public async Task CreateAsync_WithContributors_ComposesReport()
    {
        var clock = new TestClock(new DateTimeOffset(2024, 1, 12, 7, 30, 0, TimeSpan.Zero));
        var reporter = new HealthReporter(
            new IHealthContributor[]
            {
                new StubContributor(HealthComponent.CreateHealthy("database", "ok")),
                new StubContributor(HealthComponent.CreateUnhealthy("service-bus", "degraded"))
            },
            clock);

        var report = await reporter.CreateAsync(CancellationToken.None);

        Assert.Equal("management-api", report.Service);
        Assert.Equal(clock.UtcNow, report.GeneratedAt);
        Assert.Equal(2, report.Components.Count);
        Assert.False(report.IsHealthy);
        Assert.Contains(report.Components, component => component.Component == "service-bus" && !component.Healthy);
    }

    [Fact]
    public async Task CreateAsync_WhenNoContributors_AddsDefaultComponent()
    {
        var clock = new TestClock(new DateTimeOffset(2024, 1, 12, 7, 30, 0, TimeSpan.Zero));
        var reporter = new HealthReporter(Array.Empty<IHealthContributor>(), clock);

        var report = await reporter.CreateAsync(CancellationToken.None);

        Assert.True(report.IsHealthy);
        Assert.Single(report.Components);
        Assert.Equal("management-api", report.Components[0].Component);
    }

    private sealed class StubContributor : IHealthContributor
    {
        private readonly HealthComponent _component;

        public StubContributor(HealthComponent component)
        {
            _component = component;
        }

        public ValueTask<HealthComponent> CheckAsync(CancellationToken cancellationToken)
        {
            return ValueTask.FromResult(_component);
        }
    }
}
