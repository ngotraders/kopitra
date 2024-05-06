using AdminApi.Domain.DistributionGroups;
using AdminApi.Domain.IdRegistries;
using EventFlow;
using EventFlow.Extensions;

namespace AdminApi.Domain
{
    public static class EventFlowOptionsExtensions
    {
        public static IEventFlowOptions AddIdRegistries(this IEventFlowOptions options)
        {
            return options.AddEvents(new[] { typeof(IdRegistryKeyIdPairAddedEvent), typeof(IdRegistryKeyIdPairRemovedEvent) })
                          .AddCommands(new[] { typeof(IdRegistryRegisterKeyCommand) })
                          .AddCommandHandlers(new[] { typeof(IdRegistryCommandHandler) })
                          .AddSnapshots(new[] { typeof(IdRegistrySnapshot) })
                          .UseInMemoryReadStoreFor<IdRegistryReadModel>();
        }

        public static IEventFlowOptions AddDistributionGroups(this IEventFlowOptions options)
        {
            return options
                .AddEvents(new[] {
                    typeof(DistributionGroupUpdatedEvent),
                    typeof(DistributionGroupDeletedEvent),
                    typeof(DistributionGroupAdministratorAddedEvent),
                    typeof(DistributionGroupAdministratorRemovedEvent)
                })
                .AddCommands(new[] {
                    typeof(DistributionGroupCreateCommand),
                    typeof(DistributionGroupUpdateCommand),
                    typeof(DistributionGroupUpdateAdministratorsCommand)
                })
                .AddCommandHandlers(new[] { typeof(DistributionGroupCommandHandler) })
                .AddSnapshots(new[] { typeof(DistributionGroupSnapshot) })
                .UseInMemoryReadStoreFor<DistributionGroupReadModel>();
        }
    }
}
