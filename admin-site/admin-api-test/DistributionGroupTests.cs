using AdminApi.Domain.DistributionGroups;
using EventFlow.Queries;

namespace AdminApi
{
    [TestClass]
    public class DistributionGroupTests : EventFlowTestsBase
    {
        [TestMethod]
        public async Task 配信グループを作成する()
        {
            var id = DistributionGroupId.New;
            const string name = "TestName";

            await ProcessCommandAsync(new DistributionGroupCreateCommand(id, name)).ConfigureAwait(false);
            await AssertAsync(new ReadModelByIdQuery<DistributionGroupReadModel>(id), readModel =>
            {
                Assert.AreEqual(name, readModel!.Name);
            }).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task 配信グループの名前を変更する()
        {
            var id = DistributionGroupId.New;
            const string name = "TestName";

            await ProcessCommandAsync(new DistributionGroupCreateCommand(id, name)).ConfigureAwait(false);
            await AssertAsync(new ReadModelByIdQuery<DistributionGroupReadModel>(id), readModel =>
            {
                Assert.AreEqual(name, readModel!.Name);
            }).ConfigureAwait(false);

            const string newName = @"New${name}";
            await ProcessCommandAsync(new DistributionGroupUpdateCommand(id, newName)).ConfigureAwait(false);
            await AssertAsync(new ReadModelByIdQuery<DistributionGroupReadModel>(id), readModel =>
            {
                Assert.AreEqual(newName, readModel!.Name);
            }).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task 配信グループを削除する()
        {
            var id = DistributionGroupId.New;
            const string name = "TestName";

            await ProcessCommandAsync(new DistributionGroupCreateCommand(id, name)).ConfigureAwait(false);
            await AssertAsync(new ReadModelByIdQuery<DistributionGroupReadModel>(id), readModel =>
            {
                Assert.AreEqual(name, readModel!.Name);
            }).ConfigureAwait(false);

            await ProcessCommandAsync(new DistributionGroupDeleteCommand(id)).ConfigureAwait(false);
            await AssertAsync(new ReadModelByIdQuery<DistributionGroupReadModel>(id), readModel =>
            {
                Assert.IsNull(readModel);
            }).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task 配信グループに管理者を追加する()
        {
            var id = DistributionGroupId.New;
            const string name = "TestName";
            var userId = UserId.New;

            await ProcessCommandAsync(new DistributionGroupCreateCommand(id, name)).ConfigureAwait(false);
            await AssertAsync(new ReadModelByIdQuery<DistributionGroupReadModel>(id), readModel =>
            {
                Assert.AreEqual(name, readModel!.Name);
            }).ConfigureAwait(false);

            await ProcessCommandAsync(new DistributionGroupUpdateAdministratorsCommand(id, [userId])).ConfigureAwait(false);
            await AssertAsync(new ReadModelByIdQuery<DistributionGroupReadModel>(id), readModel =>
            {
                CollectionAssert.AreEquivalent(new[] { userId }, readModel!.Administrators.ToList());
            }).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task 配信グループに管理者を削除する()
        {
            var id = DistributionGroupId.New;
            const string name = "TestName";
            var userId1 = UserId.New;
            var userId2 = UserId.New;

            await ProcessCommandAsync(new DistributionGroupCreateCommand(id, name)).ConfigureAwait(false);
            await AssertAsync(new ReadModelByIdQuery<DistributionGroupReadModel>(id), readModel =>
            {
                Assert.AreEqual(name, readModel!.Name);
            }).ConfigureAwait(false);

            await ProcessCommandAsync(new DistributionGroupUpdateAdministratorsCommand(id, [userId1, userId2])).ConfigureAwait(false);
            await AssertAsync(new ReadModelByIdQuery<DistributionGroupReadModel>(id), readModel =>
            {
                CollectionAssert.AreEquivalent(new[] { userId1, userId2 }, readModel!.Administrators.ToList());
            }).ConfigureAwait(false);

            await ProcessCommandAsync(new DistributionGroupUpdateAdministratorsCommand(id, [userId2])).ConfigureAwait(false);
            await AssertAsync(new ReadModelByIdQuery<DistributionGroupReadModel>(id), readModel =>
            {
                CollectionAssert.AreEquivalent(new[] { userId2 }, readModel!.Administrators.ToList());
            }).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task 配信グループにアカウントを追加する()
        {
            var id = DistributionGroupId.New;
            const string name = "TestName";
            var accountId = AccountId.New;

            await ProcessCommandAsync(new DistributionGroupCreateCommand(id, name)).ConfigureAwait(false);
            await AssertAsync(new ReadModelByIdQuery<DistributionGroupReadModel>(id), readModel =>
            {
                Assert.AreEqual(name, readModel!.Name);
            }).ConfigureAwait(false);

            await ProcessCommandAsync(new DistributionGroupAddAccountCommand(id, accountId)).ConfigureAwait(false);
            await AssertAsync(new ReadModelByIdQuery<DistributionGroupReadModel>(id), readModel =>
            {
                CollectionAssert.AreEquivalent(new[] { accountId }, readModel!.Accounts.ToList());
            }).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task 配信グループからアカウントを削除する()
        {
            var id = DistributionGroupId.New;
            const string name = "TestName";
            var accountId = AccountId.New;

            await ProcessCommandAsync(new DistributionGroupCreateCommand(id, name)).ConfigureAwait(false);
            await AssertAsync(new ReadModelByIdQuery<DistributionGroupReadModel>(id), readModel =>
            {
                Assert.AreEqual(name, readModel!.Name);
            }).ConfigureAwait(false);

            await ProcessCommandAsync(new DistributionGroupAddAccountCommand(id, accountId)).ConfigureAwait(false);
            await AssertAsync(new ReadModelByIdQuery<DistributionGroupReadModel>(id), readModel =>
            {
                CollectionAssert.AreEquivalent(new[] { accountId }, readModel!.Accounts.ToList());
            }).ConfigureAwait(false);

            await ProcessCommandAsync(new DistributionGroupRemoveAccountCommand(id, accountId)).ConfigureAwait(false);
            await AssertAsync(new ReadModelByIdQuery<DistributionGroupReadModel>(id), readModel =>
            {
                Assert.AreEqual(0, readModel!.Accounts.Count);
            }).ConfigureAwait(false);
        }
    }
}