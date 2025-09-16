namespace Functions;

public record HealthReport(string Component, bool Healthy, string Message)
{
    public bool IsHealthy => Healthy;
}

public static class HealthReporter
{
    private const string ComponentName = "functions";

    public static HealthReport Create()
    {
        return new HealthReport(ComponentName, true, "ok");
    }
}
