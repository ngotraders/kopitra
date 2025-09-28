using System.Threading;
using System.Threading.Tasks;

namespace Kopitra.ManagementApi.Common.Cqrs;

public interface IQueryHandler<TQuery, TResponse> where TQuery : IQuery<TResponse>
{
    Task<TResponse> HandleAsync(TQuery query, CancellationToken cancellationToken);
}
