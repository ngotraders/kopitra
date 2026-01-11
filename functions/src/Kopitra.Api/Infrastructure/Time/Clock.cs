using Kopitra.Api.Common;

namespace Kopitra.Api.Infrastructure.Time;

public sealed class Clock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
