using System;
using System.Threading;
using System.Threading.Tasks;
using Kopitra.Cqrs.Queries;
using Microsoft.Extensions.DependencyInjection;

namespace Kopitra.Cqrs.Dispatching;

public sealed class QueryDispatcher : IQueryDispatcher
{
    private readonly IServiceProvider _serviceProvider;

    public QueryDispatcher(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public Task<TResult> DispatchAsync<TResult>(IQuery<TResult> query, CancellationToken cancellationToken)
    {
        if (query is null)
        {
            throw new ArgumentNullException(nameof(query));
        }

        var handlerType = typeof(IQueryHandler<,>).MakeGenericType(query.GetType(), typeof(TResult));
        var handler = _serviceProvider.GetService(handlerType) ??
                      throw new InvalidOperationException($"No query handler registered for {query.GetType().Name}.");

        var method = handlerType.GetMethod("HandleAsync") ?? throw new InvalidOperationException("HandleAsync not found on handler");
        var task = (Task<TResult>?)method.Invoke(handler, new object[] { query, cancellationToken });
        return task ?? Task.FromResult(default(TResult)!);
    }
}
