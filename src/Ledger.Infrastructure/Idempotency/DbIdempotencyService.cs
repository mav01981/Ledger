using Ledger.Domain;
using Ledger.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Ledger.Infrastructure.Idempotency;

public class DbIdempotencyService : IIdempotencyService
{
    private readonly LedgerDbContext _context;
    private readonly TimeProvider _timeProvider;

    public DbIdempotencyService(LedgerDbContext context, TimeProvider timeProvider)
    {
        _context = context;
        _timeProvider = timeProvider;
    }

    public async Task<bool> HasBeenProcessedAsync(string idempotencyKey, CancellationToken ct = default)
    {
        return await _context.IdempotencyRecords
            .AnyAsync(r => r.Key == idempotencyKey, ct);
    }

    public async Task MarkProcessedAsync(string idempotencyKey, Guid aggregateId, CancellationToken ct = default)
    {
        var record = new IdempotencyRecord
        {
            Key = idempotencyKey,
            AggregateId = aggregateId,
            ProcessedAt = _timeProvider.GetUtcNow().UtcDateTime
        };
        
        _context.IdempotencyRecords.Add(record);

        await _context.SaveChangesAsync(ct);
    }
}
