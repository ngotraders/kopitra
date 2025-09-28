using System.Threading;
using System.Threading.Tasks;
using Kopitra.Cqrs.Commands;
using Kopitra.Cqrs.EventStore;
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
    private readonly AggregateRepository<CopyTradeGroupAggregate, string> _repository;
    private readonly ICopyTradeGroupReadModelStore _readModelStore;
    private readonly IClock _clock;

    public CreateCopyTradeGroupCommandHandler(
        AggregateRepository<CopyTradeGroupAggregate, string> repository,
        ICopyTradeGroupReadModelStore readModelStore,
        IClock clock)
    {
        _repository = repository;
        _readModelStore = readModelStore;
        _clock = clock;
    }

    public async Task<CopyTradeGroupReadModel> HandleAsync(CreateCopyTradeGroupCommand command, CancellationToken cancellationToken)
    {
        var aggregate = await _repository.GetAsync(command.GroupId, cancellationToken).ConfigureAwait(false);
        aggregate.Create(command.TenantId, command.GroupId, command.Name, command.Description, command.RequestedBy, _clock.UtcNow);
        await _repository.SaveAsync(aggregate, cancellationToken).ConfigureAwait(false);
        var readModel = await _readModelStore.GetAsync(command.TenantId, command.GroupId, cancellationToken).ConfigureAwait(false);
        if (readModel is null)
        {
            throw new InvalidOperationException("Copy trade group read model missing after creation.");
        }

        return readModel;
    }
}
