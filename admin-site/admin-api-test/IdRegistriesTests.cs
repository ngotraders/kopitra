using EventFlow.Queries;
using EventFlow;
using AdminApi.Domain.IdRegistries;
using Microsoft.Extensions.DependencyInjection;

namespace AdminApi
{
    [TestClass]
    public class IdRegistriesTests
    {
        [TestMethod]
        public async Task TestMethod1()
        {
            using var services = new ServiceCollection()
                .AddLogging()
                .AddAdminApiDomain()
                .BuildServiceProvider();

            var id = IdRegistryId.New;

            const string key = "TestKey";

            var commandBus = services.GetService<ICommandBus>()!;
            var command = new IdRegistryRegisterKeyCommand(id, key);
            var executionResult = await commandBus.PublishAsync(command, CancellationToken.None)
                .ConfigureAwait(false);

            Assert.AreEqual(true, executionResult.IsSuccess);

            var queryProcessor = services.GetService<IQueryProcessor>()!;
            var query = new ReadModelByIdQuery<IdRegistryReadModel>(id);
            var exampleReadModel = await queryProcessor.ProcessAsync(query, CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsTrue(exampleReadModel.Keys.TryGetValue(key, out var generatedId));
            Assert.AreEqual(key, exampleReadModel.Ids[generatedId]);


            var command2 = new IdRegistryRemoveKeyCommand(id, key, generatedId);
            var executionResult2 = await commandBus.PublishAsync(command2, CancellationToken.None)
                .ConfigureAwait(false);

            Assert.AreEqual(true, executionResult2.IsSuccess);
        }
    }
}