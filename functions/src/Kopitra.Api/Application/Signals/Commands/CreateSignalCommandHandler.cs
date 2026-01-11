using Kopitra.Api.Domain.ValueObjects;

namespace Kopitra.Api.Application.Signals.Commands
{
    public class CreateSignalCommandHandler
    {
        // Minimal handler: echo back the provided SignalId (real implementation should
        // persist via EventFlow and emit events).
        public Task<SignalId> HandleAsync(CreateSignalCommand command)
        {
            return Task.FromResult(command.AggregateId);
        }
    }
}
