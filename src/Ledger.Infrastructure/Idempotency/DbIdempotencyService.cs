using Ledger.Domain;
using Ledger.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Ledger.Infrastructure.Idempotency;

public class DbIdempotencyService : IIdempotencyService
{
    private readonly LedgerDbContext _context;

    public DbIdempotencyService(LedgerDbContext context)
    {
        _context = context;
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
            ProcessedAt = DateTime.UtcNow
        };
        
        _context.IdempotencyRecords.Add(record);

        await _context.SaveChangesAsync(ct);
    }
}
