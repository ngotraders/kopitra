using EventFlow.Queries;
using AdminApi.Domain.IdRegistries;

namespace AdminApi
{
    [TestClass]
    public class IdRegistriesTests : EventFlowTestsBase
    {
        [TestMethod]
        public async Task IDを生成し破棄する()
        {
            var id = IdRegistryId.New;

            const string key = "TestKey";
            var generatedId = null as string;

            await ProcessCommandAsync(new IdRegistryRegisterKeyCommand(id, key)).ConfigureAwait(false);
            await AssertAsync(new ReadModelByIdQuery<IdRegistryReadModel>(id), readModel =>
            {
                Assert.IsTrue(readModel!.Keys.TryGetValue(key, out generatedId));
                Assert.AreEqual(key, readModel.Ids[generatedId]);
            }).ConfigureAwait(false);

            await ProcessCommandAsync(new IdRegistryRemoveKeyCommand(id, key, generatedId!)).ConfigureAwait(false);
            await AssertAsync(new ReadModelByIdQuery<IdRegistryReadModel>(id), readModel =>
            {
                Assert.IsFalse(readModel!.Keys.TryGetValue(key, out _));
            }).ConfigureAwait(false);
        }
    }
}