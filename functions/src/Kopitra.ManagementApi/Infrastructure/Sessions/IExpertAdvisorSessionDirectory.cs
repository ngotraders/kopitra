using System;
using System.Threading;
using System.Threading.Tasks;

namespace Kopitra.ManagementApi.Infrastructure.Sessions;

public interface IExpertAdvisorSessionDirectory
{
    ValueTask RegisterAsync(ExpertAdvisorSessionRecord record, CancellationToken cancellationToken);

    ValueTask<ExpertAdvisorSessionRecord?> GetAsync(string accountId, CancellationToken cancellationToken);
}

public sealed record ExpertAdvisorSessionRecord(
    string AccountId,
    Guid SessionId,
    string AuthKeyFingerprint,
    string? ApprovedBy,
    DateTimeOffset ApprovedAt,
    DateTimeOffset? ExpiresAt)
{
    public bool IsExpired(DateTimeOffset asOf) => ExpiresAt is { } expires && expires <= asOf;
}
