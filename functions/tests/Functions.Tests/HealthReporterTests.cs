using Functions;
using Xunit;

namespace Functions.Tests;

public class HealthReporterTests
{
    [Fact]
    public void Create_ReturnsHealthyReport()
    {
        var report = HealthReporter.Create();

        Assert.True(report.IsHealthy);
        Assert.True(report.Healthy);
        Assert.Equal("functions", report.Component);
        Assert.Equal("ok", report.Message);
    }
}
