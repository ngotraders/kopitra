using System;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Configurations;
using Microsoft.OpenApi.Models;

namespace Kopitra.ManagementApi.Infrastructure.OpenApi;

public sealed class ManagementOpenApiConfigurationOptions : OpenApiConfigurationOptions
{
    public ManagementOpenApiConfigurationOptions()
    {
        Info = new OpenApiInfo
        {
            Title = "Kopitra Management API",
            Version = "1.0.0",
            Description = "Administrative APIs for managing expert advisors, copy-trade groups, and notifications.",
            Contact = new OpenApiContact
            {
                Name = "Kopitra Support",
                Email = "support@kopitra.example",
                Url = new Uri("https://github.com/kopitra"),
            },
        };

        Servers = DefaultOpenApiConfigurationOptions.GetHostNames();
        OpenApiVersion = DefaultOpenApiConfigurationOptions.GetOpenApiVersion();
        IncludeRequestingHostName = DefaultOpenApiConfigurationOptions.IsFunctionsRuntimeEnvironmentDevelopment();
        ForceHttps = DefaultOpenApiConfigurationOptions.IsHttpsForced();
        ForceHttp = DefaultOpenApiConfigurationOptions.IsHttpForced();
    }
}
