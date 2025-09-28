using System.Threading;
using System.Threading.Tasks;

namespace Kopitra.ManagementApi.Common.Cqrs;

public interface ICommandDispatcher
{
    Task<TResponse> DispatchAsync<TResponse>(ICommand<TResponse> command, CancellationToken cancellationToken);
}
