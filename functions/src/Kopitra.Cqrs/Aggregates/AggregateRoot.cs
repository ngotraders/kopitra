using System;
using System.Collections.Generic;
using System.Linq;
using Kopitra.Cqrs.Events;

namespace Kopitra.Cqrs.Aggregates;

public abstract class AggregateRoot<TId>
{
    private readonly List<IDomainEvent> _changes = new();
    private int _version = -1;
    private bool _initialized;

    public TId Id { get; private set; } = default!;

    public int PersistedVersion => _version;

    public int CurrentVersion => _version + _changes.Count;

    public IReadOnlyCollection<IDomainEvent> GetUncommittedEvents() => _changes.AsReadOnly();

    public void ClearUncommittedEvents()
    {
        _version += _changes.Count;
        _changes.Clear();
    }

    protected void Initialize(TId id)
    {
        if (_initialized)
        {
            if (!EqualityComparer<TId>.Default.Equals(Id, id))
            {
                throw new InvalidOperationException($"Aggregate already initialized with id {Id}.");
            }

            return;
        }

        Id = id;
        _initialized = true;
    }

    public void LoadFromHistory(IEnumerable<IDomainEventEnvelope> history)
    {
        foreach (var envelope in history.OrderBy(e => e.Version))
        {
            ApplyChange(envelope.Event, false);
            _version = envelope.Version;
        }
    }

    protected void Emit(IDomainEvent domainEvent)
    {
        if (!_initialized)
        {
            throw new InvalidOperationException("Aggregate must be initialized before emitting events.");
        }

        ApplyChange(domainEvent, true);
    }

    private void ApplyChange(IDomainEvent domainEvent, bool isNew)
    {
        var method = GetType().GetMethod(
            "Apply",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
            Type.DefaultBinder,
            new[] { domainEvent.GetType() },
            null);
        if (method == null)
        {
            throw new InvalidOperationException($"Aggregate {GetType().Name} cannot apply event {domainEvent.GetType().Name}.");
        }

        method.Invoke(this, new object[] { domainEvent });

        if (isNew)
        {
            _changes.Add(domainEvent);
        }
    }

    protected void EnsureInitialized(TId id)
    {
        Initialize(id);
    }
}
