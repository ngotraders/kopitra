namespace Kopitra.ManagementApi.Diagnostics;

public interface IHealthContributor
{
    ValueTask<HealthComponent> CheckAsync(CancellationToken cancellationToken);
}
