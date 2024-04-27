using AdminApi.Domain;
using AdminApi.Domain.DistributionGroups;
using EventFlow.EventStores;
using EventFlow.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace AdminApi
{
    public static class ServiceCollectionExtension
    {
        public static IServiceCollection AddAdminApiDomain(this IServiceCollection services)
        {
            return services.AddEventFlow(options =>
            {
                options.AddDistributionGroups()
                       .UseInMemorySnapshotPersistence();
            });
        }
    }
}
