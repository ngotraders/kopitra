namespace Kopitra.ManagementApi.Diagnostics;

public sealed class StaticHealthContributor : IHealthContributor
{
    private readonly HealthComponent _component;

    public StaticHealthContributor(string component, bool healthy, string message)
    {
        _component = new HealthComponent(component, healthy, message);
    }

    public ValueTask<HealthComponent> CheckAsync(CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(_component);
    }
}
