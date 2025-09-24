using System;

namespace Kopitra.Cqrs.EventStore;

public sealed class ConcurrencyException : Exception
{
    public ConcurrencyException(string streamId, int expectedVersion, int actualVersion)
        : base($"Stream '{streamId}' expected version {expectedVersion} but found {actualVersion}.")
    {
        StreamId = streamId;
        ExpectedVersion = expectedVersion;
        ActualVersion = actualVersion;
    }

    public string StreamId { get; }

    public int ExpectedVersion { get; }

    public int ActualVersion { get; }
}
