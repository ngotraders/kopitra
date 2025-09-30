using System.Security.Claims;

namespace Kopitra.ManagementApi.Common.RequestValidation;

public sealed record AdminRequestContext(string TenantId, ClaimsPrincipal Principal);
