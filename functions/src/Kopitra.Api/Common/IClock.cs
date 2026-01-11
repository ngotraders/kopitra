namespace Kopitra.Api.Common;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
