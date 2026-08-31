using Ledger.Domain.Events;
using Ledger.Infrastructure.Data;
using Ledger.Infrastructure.EventStore;

namespace Ledger.Infrastructure.Outbox;

public class OutboxWriter
{
    private readonly LedgerDbContext _context;

    public OutboxWriter(LedgerDbContext context)
    {
        _context = context;
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
    }
}
