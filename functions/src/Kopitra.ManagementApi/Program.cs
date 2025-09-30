using Kopitra.ManagementApi.DependencyInjection;
using Kopitra.ManagementApi.Infrastructure.OpenApi;
using Microsoft.Azure.Functions.Worker.Extensions.OpenApi.Extensions;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults(worker => worker.UseNewtonsoftJson())
    .ConfigureServices(services =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.AddManagementApiCore();
        services.AddSingleton<IOpenApiConfigurationOptions, ManagementOpenApiConfigurationOptions>();
    })
    .Build();

host.Run();
