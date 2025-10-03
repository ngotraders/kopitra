namespace Kopitra.ManagementApi.Infrastructure.Messaging;

public sealed class ServiceBusOptions
{
    public string? EmulatorBaseUrl { get; set; }

    public string AdminQueueName { get; set; } = "ea-admin";
}
