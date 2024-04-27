using AdminApi.Domain.DistributionGroups;
using EventFlow;
using EventFlow.Queries;
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
                .AddAdminApiDomain()
                .BuildServiceProvider();

            var id = DistributionGroupId.New;

            const string name = "TestName";

            var commandBus = services.GetService<ICommandBus>()!;
            var command = new DistributionGroupCreateCommand(id, name);
            var executionResult = await commandBus.PublishAsync(command, CancellationToken.None)
                .ConfigureAwait(false);

            Assert.AreEqual(true, executionResult.IsSuccess);

            var queryProcessor = services.GetService<IQueryProcessor>()!;
            var query = new ReadModelByIdQuery<DistributionGroupReadModel>(id);
            var exampleReadModel = await queryProcessor.ProcessAsync(query, CancellationToken.None)
                .ConfigureAwait(false);

            Assert.AreEqual(name, exampleReadModel.Name);

            var command2 = new DistributionGroupUpdateCommand(id, @"New${name}");
            var executionResult2 = await commandBus.PublishAsync(command2, CancellationToken.None)
                .ConfigureAwait(false);
            
            Assert.AreEqual(true, executionResult2.IsSuccess);

            var query2 = new ReadModelByIdQuery<DistributionGroupReadModel>(id);
            var exampleReadModel2 = await queryProcessor.ProcessAsync(query2, CancellationToken.None)
                .ConfigureAwait(false);

            Assert.AreEqual(@"New${name}", exampleReadModel2.Name);

            var command3 = new DistributionGroupDeleteCommand(id);
            var executionResult3 = await commandBus.PublishAsync(command3, CancellationToken.None)
                .ConfigureAwait(false);

            Assert.AreEqual(true, executionResult3.IsSuccess);

            var query3 = new ReadModelByIdQuery<DistributionGroupReadModel>(id);
            var exampleReadModel3 = await queryProcessor.ProcessAsync(query2, CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsNull(exampleReadModel3);
        }
    }
}