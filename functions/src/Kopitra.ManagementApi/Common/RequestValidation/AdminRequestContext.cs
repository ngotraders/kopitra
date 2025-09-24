namespace Kopitra.ManagementApi.Common.RequestValidation;

public sealed record AdminRequestContext(string TenantId, string? IdempotencyKey);
