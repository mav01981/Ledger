using Ledger.Domain.Events;
using Ledger.Infrastructure.Data;
using Ledger.Infrastructure.EventStore;

namespace Ledger.Infrastructure.Outbox;

public class OutboxWriter
{
    private readonly LedgerDbContext _context;
    private readonly OutboxMetrics _metrics;

    public OutboxWriter(LedgerDbContext context, OutboxMetrics metrics)
    {
        _context = context;
        _metrics = metrics;
    }

    public async Task AddToOutboxAsync(IReadOnlyList<DomainEvent> events, CancellationToken ct)
    {
        foreach (var @event in events)
        {
            _context.Outbox.Add(new OutboxMessage
            {
                Id = Guid.NewGuid(),
                EventType = EventSerializer.GetEventTypeName(@event),
                Payload = EventSerializer.Serialize(@event),
                Timestamp = @event.Timestamp,
                ProcessedAt = null
            });
        }
        await _context.SaveChangesAsync(ct);
        _metrics.RecordEnqueued(events.Count);
    }
}
