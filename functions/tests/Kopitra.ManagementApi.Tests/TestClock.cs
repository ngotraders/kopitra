using Kopitra.ManagementApi.Time;

namespace Kopitra.ManagementApi.Tests;

public sealed class TestClock : IClock
{
    private DateTimeOffset _utcNow;

    public TestClock()
        : this(DateTimeOffset.UtcNow)
    {
    }

    public TestClock(DateTimeOffset utcNow)
    {
        _utcNow = utcNow;
    }

    public DateTimeOffset UtcNow
    {
        get => _utcNow;
        set => _utcNow = value;
    }

    public void Advance(TimeSpan duration)
    {
        _utcNow = _utcNow.Add(duration);
    }

    public void SetTime(DateTimeOffset value) => _utcNow = value;
}
