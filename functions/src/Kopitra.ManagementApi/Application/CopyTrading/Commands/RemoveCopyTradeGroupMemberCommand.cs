using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Aggregates;
using EventFlow.Core;
using Kopitra.ManagementApi.Common.Cqrs;
using Kopitra.ManagementApi.Domain.CopyTrading;
using Kopitra.ManagementApi.Infrastructure.ReadModels;
using Kopitra.ManagementApi.Time;

namespace Kopitra.ManagementApi.Application.CopyTrading.Commands;

public sealed record RemoveCopyTradeGroupMemberCommand(
    string TenantId,
    string GroupId,
    string MemberId,
    string RequestedBy) : ICommand<CopyTradeGroupReadModel>;

public sealed class RemoveCopyTradeGroupMemberCommandHandler : ICommandHandler<RemoveCopyTradeGroupMemberCommand, CopyTradeGroupReadModel>
{
    private readonly IAggregateStore _aggregateStore;
    private readonly ICopyTradeGroupReadModelStore _readModelStore;
    private readonly IClock _clock;

    public RemoveCopyTradeGroupMemberCommandHandler(
        IAggregateStore aggregateStore,
        ICopyTradeGroupReadModelStore readModelStore,
        IClock clock)
    {
        _aggregateStore = aggregateStore;
        _readModelStore = readModelStore;
        _clock = clock;
    }

    public async Task<CopyTradeGroupReadModel> HandleAsync(RemoveCopyTradeGroupMemberCommand command, CancellationToken cancellationToken)
    {
        var id = CopyTradeGroupId.FromBusinessId(command.GroupId);
        var timestamp = _clock.UtcNow;
        await _aggregateStore.UpdateAsync<CopyTradeGroupAggregate, CopyTradeGroupId>(
            id,
            SourceId.New,
            (aggregate, _) =>
            {
                if (!string.Equals(aggregate.TenantId, command.TenantId, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException("Tenant mismatch for copy trade group member removal.");
                }

                aggregate.RemoveMember(command.MemberId, timestamp, command.RequestedBy);
                return Task.CompletedTask;
            },
            cancellationToken).ConfigureAwait(false);

        var readModel = await _readModelStore.GetAsync(command.TenantId, command.GroupId, cancellationToken).ConfigureAwait(false);
        if (readModel is null)
        {
            var aggregateState = await _aggregateStore.LoadAsync<CopyTradeGroupAggregate, CopyTradeGroupId>(id, cancellationToken).ConfigureAwait(false);
            var members = aggregateState.Members.Values
                .OrderBy(m => m.MemberId)
                .Select(m => new CopyTradeGroupMemberReadModel(m.MemberId, m.Role, m.RiskStrategy, m.Allocation, m.UpdatedAt, m.UpdatedBy))
                .ToArray();
            return new CopyTradeGroupReadModel(aggregateState.TenantId, command.GroupId, aggregateState.GroupName, aggregateState.Description, aggregateState.CreatedBy, aggregateState.CreatedAt, members);
        }

        return readModel;
    }
}
