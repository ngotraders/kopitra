using System;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Aggregates;
using EventFlow.Core;
using Kopitra.ManagementApi.Common.Cqrs;
using Kopitra.ManagementApi.Domain.CopyTrading;
using Kopitra.ManagementApi.Infrastructure.ReadModels;
using Kopitra.ManagementApi.Time;

namespace Kopitra.ManagementApi.Application.CopyTrading.Commands;

public sealed record CreateCopyTradeGroupCommand(
    string TenantId,
    string GroupId,
    string Name,
    string? Description,
    string RequestedBy) : ICommand<CopyTradeGroupReadModel>;

public sealed class CreateCopyTradeGroupCommandHandler : ICommandHandler<CreateCopyTradeGroupCommand, CopyTradeGroupReadModel>
{
    private readonly IAggregateStore _aggregateStore;
    private readonly ICopyTradeGroupReadModelStore _readModelStore;
    private readonly IClock _clock;

    public CreateCopyTradeGroupCommandHandler(
        IAggregateStore aggregateStore,
        ICopyTradeGroupReadModelStore readModelStore,
        IClock clock)
    {
        _aggregateStore = aggregateStore;
        _readModelStore = readModelStore;
        _clock = clock;
    }

    public async Task<CopyTradeGroupReadModel> HandleAsync(CreateCopyTradeGroupCommand command, CancellationToken cancellationToken)
    {
        var id = CopyTradeGroupId.FromBusinessId(command.GroupId);
        var timestamp = _clock.UtcNow;
        await _aggregateStore.UpdateAsync<CopyTradeGroupAggregate, CopyTradeGroupId>(
            id,
            SourceId.New,
            (aggregate, _) =>
            {
                aggregate.Create(command.TenantId, command.GroupId, command.Name, command.Description, command.RequestedBy, timestamp);
                return Task.CompletedTask;
            },
            cancellationToken).ConfigureAwait(false);

        var readModel = await _readModelStore.GetAsync(command.TenantId, command.GroupId, cancellationToken).ConfigureAwait(false);
        if (readModel is null)
        {
            var aggregateState = await _aggregateStore.LoadAsync<CopyTradeGroupAggregate, CopyTradeGroupId>(id, cancellationToken).ConfigureAwait(false);
            return new CopyTradeGroupReadModel(aggregateState.TenantId, command.GroupId, aggregateState.GroupName, aggregateState.Description, aggregateState.CreatedBy, aggregateState.CreatedAt, Array.Empty<CopyTradeGroupMemberReadModel>());
        }

        return readModel;
    }
}
