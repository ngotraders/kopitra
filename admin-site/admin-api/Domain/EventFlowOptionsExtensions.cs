using AdminApi.Domain.DistributionGroups;
using AdminApi.Domain.IdRegistries;
using EventFlow;
using EventFlow.Extensions;

namespace AdminApi.Domain
{
    public static class EventFlowOptionsExtensions
    {
        public static IEventFlowOptions AddDistributionGroups(this IEventFlowOptions options)
        {
            return options.AddEvents(new[] { typeof(DistributionGroupUpdatedEvent), typeof(DistributionGroupDeletedEvent) })
                          .AddCommands(new[] { typeof(DistributionGroupCreateCommand) })
                          .AddCommandHandlers(new[] { typeof(DistributionGroupCommandHandler) })
                          .AddSnapshots(new[] { typeof(DistributionGroupSnapshot) })
                          .UseInMemoryReadStoreFor<DistributionGroupReadModel>();
        }
    }
}
