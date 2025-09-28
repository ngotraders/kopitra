using System;
using System.Security.Cryptography;
using System.Text;
using EventFlow.Core;

namespace Kopitra.ManagementApi.Domain.CopyTrading;

public sealed class CopyTradeGroupId : Identity<CopyTradeGroupId>
{
    public CopyTradeGroupId(string value) : base(value)
    {
    }

    public static CopyTradeGroupId FromBusinessId(string businessId)
    {
        if (string.IsNullOrWhiteSpace(businessId))
        {
            throw new ArgumentException("Business identifier cannot be empty.", nameof(businessId));
        }

        var normalized = businessId.Trim().ToLowerInvariant();
        var guid = CreateDeterministicGuid("copytradegroup", normalized);
        return With($"copytradegroup-{guid:D}");
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
