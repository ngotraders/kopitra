using System;

namespace Kopitra.ManagementApi.Infrastructure.AdminUsers;

public sealed record AdminUserCredential(string TenantId, string Email, string PasswordHash, DateTimeOffset UpdatedAt);
