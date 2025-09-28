using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Kopitra.ManagementApi.Common.Cqrs;

public sealed class CommandDispatcher : ICommandDispatcher
{
    private readonly IServiceProvider _serviceProvider;

    public CommandDispatcher(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public Task<TResponse> DispatchAsync<TResponse>(ICommand<TResponse> command, CancellationToken cancellationToken)
    {
        var handlerType = typeof(ICommandHandler<,>).MakeGenericType(command.GetType(), typeof(TResponse));
        dynamic handler = _serviceProvider.GetRequiredService(handlerType);
        return handler.HandleAsync((dynamic)command, cancellationToken);
    }
}
