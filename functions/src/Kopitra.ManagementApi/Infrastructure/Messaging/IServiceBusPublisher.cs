using System.Threading;
using System.Threading.Tasks;

namespace Kopitra.ManagementApi.Infrastructure.Messaging;

public interface IServiceBusPublisher
{
    Task PublishAsync(string topicName, object payload, CancellationToken cancellationToken);
}
