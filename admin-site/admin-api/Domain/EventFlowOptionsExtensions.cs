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
                    typeof(DistributionGroupAdministratorRemovedEvent),
                    typeof(DistributionGroupAccountAddedEvent),
                    typeof(DistributionGroupAccountRemovedEvent),
                })
                .AddCommands(new[] {
                    typeof(DistributionGroupCreateCommand),
                    typeof(DistributionGroupUpdateCommand),
                    typeof(DistributionGroupDeleteCommand),
                    typeof(DistributionGroupUpdateAdministratorsCommand),
                    typeof(DistributionGroupAddAccountCommand),
                    typeof(DistributionGroupRemoveAccountCommand),
                })
                .AddCommandHandlers(new[] { typeof(DistributionGroupCommandHandler) })
                .AddSnapshots(new[] { typeof(DistributionGroupSnapshot) })
                .UseInMemoryReadStoreFor<DistributionGroupReadModel>();
        }
    }
}
