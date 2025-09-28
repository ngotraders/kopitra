using System.Threading;
using System.Threading.Tasks;
using Kopitra.Cqrs.Queries;

namespace Kopitra.Cqrs.Dispatching;

public interface IQueryDispatcher
{
    Task<TResult> DispatchAsync<TResult>(IQuery<TResult> query, CancellationToken cancellationToken);
}
