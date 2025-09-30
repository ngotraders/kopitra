using System;
using System.Security.Cryptography;
using System.Text;
using EventFlow.Core;

namespace Kopitra.ManagementApi.Domain.ExpertAdvisors;

public sealed class ExpertAdvisorId : Identity<ExpertAdvisorId>
{
    public ExpertAdvisorId(string value) : base(value)
    {
    }

    public static ExpertAdvisorId FromBusinessId(string businessId)
    {
        if (string.IsNullOrWhiteSpace(businessId))
        {
            throw new ArgumentException("Business identifier cannot be empty.", nameof(businessId));
        }

        var normalized = businessId.Trim().ToLowerInvariant();
        var guid = CreateDeterministicGuid("expertadvisor", normalized);
        return With($"expertadvisor-{guid:D}");
    }

    private static Guid CreateDeterministicGuid(string scope, string value)
    {
        using var sha1 = SHA1.Create();
        var bytes = Encoding.UTF8.GetBytes($"{scope}:{value}");
        var hash = sha1.ComputeHash(bytes);
        var guidBytes = new byte[16];
        Array.Copy(hash, guidBytes, 16);
        return new Guid(guidBytes);
    }
}
