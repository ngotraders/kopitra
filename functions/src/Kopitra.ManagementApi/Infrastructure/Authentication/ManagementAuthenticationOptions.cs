using System;

namespace Kopitra.ManagementApi.Infrastructure.Authentication;

public sealed class ManagementAuthenticationOptions
{
    public string Authority { get; set; } = string.Empty;

    public string? MetadataAddress { get; set; }

    public string[] Audiences { get; set; } = Array.Empty<string>();

    public string[]? ValidIssuers { get; set; }

    public TimeSpan? ClockSkew { get; set; }
}
