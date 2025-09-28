using System.Threading;
using System.Threading.Tasks;
using Kopitra.Cqrs.Commands;
using Kopitra.Cqrs.EventStore;
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
    private readonly AggregateRepository<CopyTradeGroupAggregate, string> _repository;
    private readonly ICopyTradeGroupReadModelStore _readModelStore;
    private readonly IClock _clock;

    public RemoveCopyTradeGroupMemberCommandHandler(
        AggregateRepository<CopyTradeGroupAggregate, string> repository,
        ICopyTradeGroupReadModelStore readModelStore,
        IClock clock)
    {
        _repository = repository;
        _readModelStore = readModelStore;
        _clock = clock;
    }

    public async Task<CopyTradeGroupReadModel> HandleAsync(RemoveCopyTradeGroupMemberCommand command, CancellationToken cancellationToken)
    {
        var aggregate = await _repository.GetAsync(command.GroupId, cancellationToken).ConfigureAwait(false);
        if (!string.Equals(aggregate.TenantId, command.TenantId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Tenant mismatch for copy trade group member removal.");
        }

        aggregate.RemoveMember(command.MemberId, _clock.UtcNow, command.RequestedBy);
        await _repository.SaveAsync(aggregate, cancellationToken).ConfigureAwait(false);
        var readModel = await _readModelStore.GetAsync(command.TenantId, command.GroupId, cancellationToken).ConfigureAwait(false);
        if (readModel is null)
        {
            throw new InvalidOperationException("Copy trade group read model missing after member removal.");
        }

        return readModel;
    }
}
