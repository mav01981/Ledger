using System.Text.Json;
using Ledger.Domain;
using Ledger.Domain.Events;
using Ledger.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Ledger.Infrastructure.EventStore;

public class PostgresEventStore : IEventStore
{
    private readonly LedgerDbContext _context;

    public PostgresEventStore(LedgerDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<DomainEvent>> ReadStreamAsync(Guid streamId, long fromVersion = -1, CancellationToken ct = default)
    {
        var query = _context.Events
            .Where(e => e.StreamId == streamId && e.StreamPosition > fromVersion)
            .OrderBy(e => e.StreamPosition);

        var records = await query.ToListAsync(ct);
        return [.. records.Select(r => EventSerializer.Deserialize(r.EventType, r.Payload))];
    }

    public async Task AppendToStreamAsync(Guid streamId, IReadOnlyList<DomainEvent> events, long expectedVersion, CancellationToken ct = default)
    {
        // Optimistic concurrency check
        var lastPosition = await _context.Events
            .Where(e => e.StreamId == streamId)
            .MaxAsync(e => (long?)e.StreamPosition, ct) ?? -1;

        if (expectedVersion != -1 && lastPosition != expectedVersion)
        {
            throw new InvalidOperationException(
                $"Concurrency conflict on stream {streamId}. Expected version {expectedVersion}, but stream is at {lastPosition}.");
        }

        var lastGlobalPosition = await _context.Events
            .MaxAsync(e => (long?)e.GlobalPosition, ct) ?? -1;

        var records = new List<EventRecord>();

        foreach (var @event in events)
        {
            lastPosition++;
            lastGlobalPosition++;
            records.Add(new EventRecord
            {
                StreamId = streamId,
                StreamPosition = lastPosition,
                EventType = EventSerializer.GetEventTypeName(@event),
                Payload = EventSerializer.Serialize(@event),
                Timestamp = @event.Timestamp,
                GlobalPosition = lastGlobalPosition
            });

            // Write to outbox in same transaction
            _context.Outbox.Add(new OutboxMessage
            {
                Id = Guid.NewGuid(),
                EventType = EventSerializer.GetEventTypeName(@event),
                Payload = EventSerializer.Serialize(@event),
                Timestamp = @event.Timestamp,
                ProcessedAt = null
            });
        }

        await _context.Events.AddRangeAsync(records, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<DomainEvent>> ReadAllAsync(long afterPosition = -1, CancellationToken ct = default)
    {
        var query = _context.Events
            .Where(e => e.GlobalPosition > afterPosition)
            .OrderBy(e => e.GlobalPosition);

        var records = await query.ToListAsync(ct);
        return records.Select(r => EventSerializer.Deserialize(r.EventType, r.Payload)).ToList();
    }
}
