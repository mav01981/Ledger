using Ledger.Domain.Events;

namespace Ledger.Domain;

public abstract class AggregateRoot
{
    public Guid Id { get; protected set; }
    public long Version { get; protected set; } = -1;

    private readonly List<DomainEvent> _uncommittedEvents = new();
    public IReadOnlyList<DomainEvent> UncommittedEvents => _uncommittedEvents;

    protected abstract void When(DomainEvent @event);

    protected void Apply(DomainEvent @event)
    {
        When(@event);
        Version++;
        _uncommittedEvents.Add(@event with { StreamPosition = Version });
    }

    public void LoadFromHistory(IEnumerable<DomainEvent> events)
    {
        foreach (var @event in events)
        {
            When(@event);
            Version = @event.StreamPosition;
        }
    }

    public void ClearUncommittedEvents() => _uncommittedEvents.Clear();
}
