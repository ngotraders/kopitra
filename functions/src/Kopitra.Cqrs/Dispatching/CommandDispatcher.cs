using System;
using System.Threading;
using System.Threading.Tasks;
using Kopitra.Cqrs.Commands;
using Microsoft.Extensions.DependencyInjection;

namespace Kopitra.Cqrs.Dispatching;

public sealed class CommandDispatcher : ICommandDispatcher
{
    private readonly IServiceProvider _serviceProvider;

    public CommandDispatcher(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public Task<TResult> DispatchAsync<TResult>(ICommand<TResult> command, CancellationToken cancellationToken)
    {
        if (command is null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        var handlerType = typeof(ICommandHandler<,>).MakeGenericType(command.GetType(), typeof(TResult));
        var handler = _serviceProvider.GetService(handlerType) ??
                      throw new InvalidOperationException($"No command handler registered for {command.GetType().Name}.");

        var method = handlerType.GetMethod("HandleAsync") ?? throw new InvalidOperationException("HandleAsync not found on handler");
        var task = (Task<TResult>?)method.Invoke(handler, new object[] { command, cancellationToken });
        return task ?? Task.FromResult(default(TResult)!);
    }
}
