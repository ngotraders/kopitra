using AdminApi.Domain.DistributionGroups;
using EventFlow;
using EventFlow.Queries;
using Microsoft.Extensions.DependencyInjection;

namespace AdminApi
{
    [TestClass]
    public class DistributionGroupTests
    {
        private ServiceProvider? services;

        [TestInitialize]
        public void Initialize()
        {
            services = new ServiceCollection()
                .AddLogging()
                .AddAdminApiDomain()
                .BuildServiceProvider();
        }

        [TestCleanup]
        public void Cleanup()
        {
            services?.Dispose();
        }

        [TestMethod]
        public async Task 配信グループを作成する()
        {
            var commandBus = services!.GetService<ICommandBus>()!;
            var queryProcessor = services!.GetService<IQueryProcessor>()!;

            var id = DistributionGroupId.New;

            const string name = "TestName";

            var createCommand = new DistributionGroupCreateCommand(id, name);
            var createResult = await commandBus.PublishAsync(createCommand, CancellationToken.None)
                .ConfigureAwait(false);

            Assert.AreEqual(true, createResult.IsSuccess);

            var query = new ReadModelByIdQuery<DistributionGroupReadModel>(id);
            var readModel = await queryProcessor.ProcessAsync(query, CancellationToken.None)
                .ConfigureAwait(false);

            Assert.AreEqual(name, readModel.Name);
        }

        [TestMethod]
        public async Task 配信グループの名前を変更する()
        {
            var commandBus = services!.GetService<ICommandBus>()!;
            var queryProcessor = services!.GetService<IQueryProcessor>()!;

            var id = DistributionGroupId.New;

            const string name = "TestName";

            var createCommand = new DistributionGroupCreateCommand(id, name);
            var createResult = await commandBus.PublishAsync(createCommand, CancellationToken.None)
                .ConfigureAwait(false);

            Assert.AreEqual(true, createResult.IsSuccess);

            {
                var query = new ReadModelByIdQuery<DistributionGroupReadModel>(id);
                var readModel = await queryProcessor.ProcessAsync(query, CancellationToken.None)
                    .ConfigureAwait(false);

                Assert.AreEqual(name, readModel.Name);
            }

            const string newName = @"New${name}";

            var updateCommand = new DistributionGroupUpdateCommand(id, newName);
            var updateResult = await commandBus.PublishAsync(updateCommand, CancellationToken.None)
                .ConfigureAwait(false);

            Assert.AreEqual(true, updateResult.IsSuccess);

            {
                var query = new ReadModelByIdQuery<DistributionGroupReadModel>(id);
                var readModel = await queryProcessor.ProcessAsync(query, CancellationToken.None)
                    .ConfigureAwait(false);

                Assert.AreEqual(newName, readModel.Name);
            }
        }

        [TestMethod]
        public async Task 配信グループを削除する()
        {
            var commandBus = services!.GetService<ICommandBus>()!;
            var queryProcessor = services!.GetService<IQueryProcessor>()!;

            var id = DistributionGroupId.New;

            const string name = "TestName";

            var createCommand = new DistributionGroupCreateCommand(id, name);
            var createResult = await commandBus.PublishAsync(createCommand, CancellationToken.None)
                .ConfigureAwait(false);

            Assert.AreEqual(true, createResult.IsSuccess);

            {
                var query = new ReadModelByIdQuery<DistributionGroupReadModel>(id);
                var readModel = await queryProcessor.ProcessAsync(query, CancellationToken.None)
                    .ConfigureAwait(false);

                Assert.AreEqual(name, readModel.Name);
            }

            var deleteCommand = new DistributionGroupDeleteCommand(id);
            var deleteResult = await commandBus.PublishAsync(deleteCommand, CancellationToken.None)
                .ConfigureAwait(false);

            Assert.AreEqual(true, deleteResult.IsSuccess);

            {
                var query = new ReadModelByIdQuery<DistributionGroupReadModel>(id);
                var readModel = await queryProcessor.ProcessAsync(query, CancellationToken.None)
                    .ConfigureAwait(false);

                Assert.IsNull(readModel);
            }
        }

        [TestMethod]
        public async Task 配信グループに管理者を追加する()
        {
            var commandBus = services!.GetService<ICommandBus>()!;
            var queryProcessor = services!.GetService<IQueryProcessor>()!;

            var id = DistributionGroupId.New;
            var userId = UserId.New;

            const string name = "TestName";

            var createCommand = new DistributionGroupCreateCommand(id, name);
            var createResult = await commandBus.PublishAsync(createCommand, CancellationToken.None)
                .ConfigureAwait(false);

            Assert.AreEqual(true, createResult.IsSuccess);

            {
                var query = new ReadModelByIdQuery<DistributionGroupReadModel>(id);
                var readModel = await queryProcessor.ProcessAsync(query, CancellationToken.None)
                    .ConfigureAwait(false);

                Assert.AreEqual(name, readModel.Name);
            }

            var addAdminCommand = new DistributionGroupUpdateAdministratorsCommand(id, [userId]);
            var addAdminResult = await commandBus.PublishAsync(addAdminCommand, CancellationToken.None)
                .ConfigureAwait(false);

            Assert.AreEqual(true, addAdminResult.IsSuccess);

            {
                var query = new ReadModelByIdQuery<DistributionGroupReadModel>(id);
                var readModel = await queryProcessor.ProcessAsync(query, CancellationToken.None)
                    .ConfigureAwait(false);

                CollectionAssert.AreEquivalent(new[] { userId }, readModel.Administrators.ToList());
            }
        }

        [TestMethod]
        public async Task 配信グループに管理者を削除する()
        {
            var commandBus = services!.GetService<ICommandBus>()!;
            var queryProcessor = services!.GetService<IQueryProcessor>()!;

            var id = DistributionGroupId.New;
            var userId1 = UserId.New;
            var userId2 = UserId.New;

            const string name = "TestName";

            var createCommand = new DistributionGroupCreateCommand(id, name);
            var createResult = await commandBus.PublishAsync(createCommand, CancellationToken.None)
                .ConfigureAwait(false);

            Assert.AreEqual(true, createResult.IsSuccess);

            {
                var query = new ReadModelByIdQuery<DistributionGroupReadModel>(id);
                var readModel = await queryProcessor.ProcessAsync(query, CancellationToken.None)
                    .ConfigureAwait(false);

                Assert.AreEqual(name, readModel.Name);
            }

            var addAdminsCommand = new DistributionGroupUpdateAdministratorsCommand(id, [userId1, userId2]);
            var addAdminsResult = await commandBus.PublishAsync(addAdminsCommand, CancellationToken.None)
                .ConfigureAwait(false);

            Assert.AreEqual(true, addAdminsResult.IsSuccess);

            {
                var query = new ReadModelByIdQuery<DistributionGroupReadModel>(id);
                var readModel = await queryProcessor.ProcessAsync(query, CancellationToken.None)
                    .ConfigureAwait(false);

                CollectionAssert.AreEquivalent(new[] { userId1, userId2 }, readModel.Administrators.ToList());
            }

            var removeAdminCommand = new DistributionGroupUpdateAdministratorsCommand(id, [userId2]);
            var removeAdminResult = await commandBus.PublishAsync(removeAdminCommand, CancellationToken.None)
                .ConfigureAwait(false);

            Assert.AreEqual(true, removeAdminResult.IsSuccess);

            {
                var query = new ReadModelByIdQuery<DistributionGroupReadModel>(id);
                var readModel = await queryProcessor.ProcessAsync(query, CancellationToken.None)
                    .ConfigureAwait(false);

                CollectionAssert.AreEquivalent(new[] { userId2 }, readModel.Administrators.ToList());
            }
        }
    }
}