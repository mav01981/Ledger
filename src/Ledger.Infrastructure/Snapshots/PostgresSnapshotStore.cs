using System.Text.Json;
using Ledger.Domain;
using Ledger.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Ledger.Infrastructure.Snapshots;

public class PostgresSnapshotStore : ISnapshotStore
{
    private readonly LedgerDbContext _context;

    public PostgresSnapshotStore(LedgerDbContext context)
    {
        _context = context;
    }

    public async Task<AccountSnapshot?> GetLatestAsync(Guid accountId, CancellationToken ct = default)
    {
        var record = await _context.Snapshots
            .Where(s => s.AggregateId == accountId)
            .OrderByDescending(s => s.Version)
            .FirstOrDefaultAsync(ct);

        if (record is null) return null;

        var state = JsonSerializer.Deserialize<SnapshotState>(record.State);
        
        if (state is null) return null;

        return new AccountSnapshot(
            record.AggregateId,
            record.Version,
            state.AccountType,
            state.Status,
            state.Balance,
            record.SnapshottedAt
        );
    }

    public async Task SaveAsync(AccountSnapshot snapshot, CancellationToken ct = default)
    {
        var state = new SnapshotState
        {
            AccountType = snapshot.AccountType,
            Status = snapshot.AccountStatus,
            Balance = snapshot.Balance
        };

        var record = new SnapshotRecord
        {
            AggregateId = snapshot.AccountId,
            Version = snapshot.Version,
            State = JsonSerializer.Serialize(state),
            SnapshottedAt = snapshot.SnapshottedAt
        };

        _context.Snapshots.Add(record);
        await _context.SaveChangesAsync(ct);
    }

    private class SnapshotState
    {
        public AccountType AccountType { get; set; }
        public AccountStatus Status { get; set; }
        public Money Balance { get; set; }
    }
}
