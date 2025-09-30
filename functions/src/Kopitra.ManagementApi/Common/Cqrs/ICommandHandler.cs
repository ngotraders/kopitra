using System.Threading;
using System.Threading.Tasks;

namespace Kopitra.ManagementApi.Common.Cqrs;

public interface ICommandHandler<TCommand, TResponse> where TCommand : ICommand<TResponse>
{
    Task<TResponse> HandleAsync(TCommand command, CancellationToken cancellationToken);
}
