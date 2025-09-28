using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Kopitra.ManagementApi.Common.Cqrs;

public sealed class QueryDispatcher : IQueryDispatcher
{
    private readonly IServiceProvider _serviceProvider;

    public QueryDispatcher(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public Task<TResponse> DispatchAsync<TResponse>(IQuery<TResponse> query, CancellationToken cancellationToken)
    {
        var handlerType = typeof(IQueryHandler<,>).MakeGenericType(query.GetType(), typeof(TResponse));
        dynamic handler = _serviceProvider.GetRequiredService(handlerType);
        return handler.HandleAsync((dynamic)query, cancellationToken);
    }
}
