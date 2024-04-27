using EventFlow.Queries;
using EventFlow;
using AdminApi.Domain.DistributionGroups;
using EventFlow.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace AdminApi
{
    [TestClass]
    public class DistributionGroupTests
    {
        [TestMethod]
        public async Task TestMethod1()
        {
            using var services = new ServiceCollection()
                .AddLogging()
                .AddEventFlow(options =>
                {
                    options.AddEvents([typeof(DistributionGroupEvent)])
                           .AddCommands([typeof(DistributionGroupCommand)])
                           .AddCommandHandlers([typeof(DistributionGroupCommandHandler)])
                           .UseInMemoryReadStoreFor<DistributionGroupReadModel>();
                })
                .BuildServiceProvider();

            // Create a new identity for our aggregate root
            var id = DistributionGroupId.New;

            // Define some important value
            const int magicNumber = 42;

            // Resolve the command bus and use it to publish a command
            var commandBus = services.GetService<ICommandBus>()!;
            var command = new DistributionGroupCommand(id, magicNumber);
            var executionResult = await commandBus.PublishAsync(command, CancellationToken.None)
                .ConfigureAwait(false);

            // Verify that we didn't trigger our domain validation
            Assert.AreEqual(true, executionResult.IsSuccess);

            // Resolve the query handler and use the built-in query for fetching
            // read models by identity to get our read model representing the
            // state of our aggregate root
            var queryProcessor = services.GetService<IQueryProcessor>()!;
            var query = new ReadModelByIdQuery<DistributionGroupReadModel>(id);
            var exampleReadModel = await queryProcessor.ProcessAsync(query, CancellationToken.None)
                .ConfigureAwait(false);

            // Verify that the read model has the expected magic number
            Assert.AreEqual(42, exampleReadModel.MagicNumber);
        }
    }
}