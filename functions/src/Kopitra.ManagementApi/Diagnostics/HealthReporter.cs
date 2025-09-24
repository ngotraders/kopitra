using System.Collections.Generic;
using Kopitra.ManagementApi.Time;

namespace Kopitra.ManagementApi.Diagnostics;

public sealed class HealthReporter
{
    private readonly IEnumerable<IHealthContributor> _contributors;
    private readonly IClock _clock;
    private const string ServiceName = "management-api";

    public HealthReporter(IEnumerable<IHealthContributor> contributors, IClock clock)
    {
        _contributors = contributors;
        _clock = clock;
    }

    public async ValueTask<HealthReport> CreateAsync(CancellationToken cancellationToken)
    {
        var components = new List<HealthComponent>();

        foreach (var contributor in _contributors)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var component = await contributor.CheckAsync(cancellationToken);
            components.Add(component);
        }

        if (components.Count == 0)
        {
            components.Add(HealthComponent.CreateHealthy(ServiceName, "No health contributors were registered."));
        }

        return new HealthReport(ServiceName, _clock.UtcNow, components);
    }
}
