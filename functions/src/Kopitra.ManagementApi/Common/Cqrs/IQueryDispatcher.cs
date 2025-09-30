using System.Threading;
using System.Threading.Tasks;

namespace Kopitra.ManagementApi.Common.Cqrs;

public interface IQueryDispatcher
{
    Task<TResponse> DispatchAsync<TResponse>(IQuery<TResponse> query, CancellationToken cancellationToken);
}
