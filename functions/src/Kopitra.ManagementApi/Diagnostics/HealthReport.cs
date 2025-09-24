using System.Collections.Generic;
using System.Linq;

namespace Kopitra.ManagementApi.Diagnostics;

public sealed record HealthReport(string Service, DateTimeOffset GeneratedAt, IReadOnlyList<HealthComponent> Components)
{
    public bool IsHealthy => Components.All(component => component.Healthy);
}
