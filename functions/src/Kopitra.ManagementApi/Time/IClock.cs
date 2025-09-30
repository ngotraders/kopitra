namespace Kopitra.ManagementApi.Time;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
