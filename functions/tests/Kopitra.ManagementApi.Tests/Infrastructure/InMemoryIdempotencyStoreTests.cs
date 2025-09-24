using System.Net;
using Kopitra.ManagementApi.Infrastructure;
using Kopitra.ManagementApi.Tests;
using Xunit;

namespace Kopitra.ManagementApi.Tests.Infrastructure;

public class InMemoryIdempotencyStoreTests
{
    [Fact]
    public async Task SaveAndRetrieve_PreservesRecordUntilExpiration()
    {
        var clock = new TestClock(new DateTimeOffset(2024, 1, 12, 7, 30, 0, TimeSpan.Zero));
        var store = new InMemoryIdempotencyStore<string>(TimeSpan.FromMinutes(10), clock);
        var record = new IdempotencyRecord<string>(HttpStatusCode.Accepted, clock.UtcNow, "payload");

        await store.SaveAsync("scope", "key", record, CancellationToken.None);

        var retrieved = await store.TryGetAsync("scope", "key", CancellationToken.None);
        Assert.NotNull(retrieved);
        Assert.Equal("payload", retrieved!.Response);

        clock.Advance(TimeSpan.FromMinutes(11));

        var expired = await store.TryGetAsync("scope", "key", CancellationToken.None);
        Assert.Null(expired);
    }
}
