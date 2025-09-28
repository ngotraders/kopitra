using System.Threading;
using System.Threading.Tasks;

namespace Kopitra.Cqrs.Commands;

public interface ICommandHandler<TCommand, TResult> where TCommand : ICommand<TResult>
{
    Task<TResult> HandleAsync(TCommand command, CancellationToken cancellationToken);
}
