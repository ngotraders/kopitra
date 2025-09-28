using System.Threading;
using System.Threading.Tasks;
using Kopitra.Cqrs.Commands;

namespace Kopitra.Cqrs.Dispatching;

public interface ICommandDispatcher
{
    Task<TResult> DispatchAsync<TResult>(ICommand<TResult> command, CancellationToken cancellationToken);
}
