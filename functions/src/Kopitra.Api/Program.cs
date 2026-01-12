using EventFlow.EventStores;
using Kopitra.Api.Application;
using Kopitra.Api.Application.Users.Services;
using Kopitra.Api.Common;
using Kopitra.Api.Infrastructure;
using Kopitra.Api.Infrastructure.EventFlow;
using Kopitra.Api.Infrastructure.Security;
using Kopitra.Api.Infrastructure.Time;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Abstractions;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Configurations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = FunctionsApplication.CreateBuilder(args);

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights()
    .Configure<JsonSerializerOptions>(options =>
    {
        options.AllowTrailingCommas = true;
        options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.PropertyNameCaseInsensitive = true;
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
    })
    .AddSingleton<IOpenApiConfigurationOptions>(_ =>
    {
        var options = new OpenApiConfigurationOptions()
        {
            Info = new OpenApiInfo()
            {
                Version = DefaultOpenApiConfigurationOptions.GetOpenApiDocVersion(),
                Title = DefaultOpenApiConfigurationOptions.GetOpenApiDocTitle(),
                Description = DefaultOpenApiConfigurationOptions.GetOpenApiDocDescription(),
                License = new OpenApiLicense()
                {
                    Name = "MIT",
                    Url = new Uri("http://opensource.org/licenses/MIT"),
                }
            },
            Servers = DefaultOpenApiConfigurationOptions.GetHostNames(),
            OpenApiVersion = DefaultOpenApiConfigurationOptions.GetOpenApiVersion(),
            IncludeRequestingHostName = DefaultOpenApiConfigurationOptions.IsFunctionsRuntimeEnvironmentDevelopment(),
            ForceHttps = DefaultOpenApiConfigurationOptions.IsHttpsForced(),
            ForceHttp = DefaultOpenApiConfigurationOptions.IsHttpForced(),
        };

        return options;
    })
    .AddKopitra<KopitraDbContextProvider>()
    .AddSingleton<KopitraDbContextProvider>()
    .AddSingleton<IClock, Clock>()
    // User management services
    .AddSingleton<IAuthorizationService, AuthorizationService>()
    // Authentication services
    .AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>()
    .AddSingleton<ITokenService, JwtTokenService>()
    .AddSingleton<IHttpRequestDataAccessor, AsyncLocalHttpRequestDataAccessor>()
    .AddSingleton<IMetadataProvider, EventFlowMetadataProvider>();

builder.Build().Run();
