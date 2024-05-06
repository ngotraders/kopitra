using AdminApi.Domain.DistributionGroups;
using EventFlow;
using EventFlow.Aggregates;
using EventFlow.Aggregates.ExecutionResults;
using EventFlow.Commands;
using EventFlow.Core;
using EventFlow.Queries;
using EventFlow.ReadStores;
using Microsoft.Extensions.DependencyInjection;

namespace AdminApi
{
    public class EventFlowTestsBase
    {
        protected ServiceProvider? services;

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

        protected async Task ProcessCommandAsync<TAggregate, TIdentity, TExecutionResult>(ICommand<TAggregate, TIdentity, TExecutionResult> command)
            where TAggregate : IAggregateRoot<TIdentity>
            where TIdentity : IIdentity
            where TExecutionResult : IExecutionResult
        {
            var commandBus = services!.GetService<ICommandBus>()!;
            var createResult = await commandBus.PublishAsync(command, CancellationToken.None)
                .ConfigureAwait(false);

            Assert.AreEqual(true, createResult.IsSuccess);
        }

        protected async Task AssertAsync<TReadModel>(IQuery<TReadModel> query, Action<TReadModel?> assertion)
            where TReadModel : class, IReadModel
        {
            var queryProcessor = services!.GetService<IQueryProcessor>()!;
            var readModel = await queryProcessor.ProcessAsync(query, CancellationToken.None)
                .ConfigureAwait(false);
            assertion.Invoke(readModel);
        }
    }
}