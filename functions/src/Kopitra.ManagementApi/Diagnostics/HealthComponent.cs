namespace Kopitra.ManagementApi.Diagnostics;

public sealed record HealthComponent(string Component, bool Healthy, string Message)
{
    public static HealthComponent CreateHealthy(string component, string message) => new(component, true, message);

    public static HealthComponent CreateUnhealthy(string component, string message) => new(component, false, message);
}
