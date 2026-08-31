using System.Text.Json;
using Ledger.Domain;
using Ledger.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Ledger.Infrastructure.Transactions;

public class PostgresTransactionStore : ITransactionStore
{
    private readonly LedgerDbContext _context;

    public PostgresTransactionStore(LedgerDbContext context)
    {
        _context = context;
    }

    public async Task<Transfer?> GetByIdAsync(Guid transactionId, CancellationToken ct = default)
    {
        var record = await _context.Transfers
            .FirstOrDefaultAsync(t => t.TransactionId == transactionId, ct);

        if (record == null) return null;

        var lines = JsonSerializer.Deserialize<List<TransactionLine>>(record.Lines);
        return new Transfer
        {
            TransactionId = record.TransactionId,
            Lines = lines ?? new List<TransactionLine>(),
            Status = record.Status,
            CreatedAt = record.CreatedAt
        };
    }

    public async Task SaveAsync(Transfer transfer, CancellationToken ct = default)
    {
        var existing = await _context.Transfers
            .FirstOrDefaultAsync(t => t.TransactionId == transfer.TransactionId, ct);

        if (existing != null)
        {
            // Update existing transfer (e.g. marking it as Reversed)
            existing.Lines = JsonSerializer.Serialize(transfer.Lines);
            existing.Status = transfer.Status;
            existing.CreatedAt = transfer.CreatedAt;
        }
        else
        {
            var record = new TransferRecord
            {
                TransactionId = transfer.TransactionId,
                Lines = JsonSerializer.Serialize(transfer.Lines),
                Status = transfer.Status,
                CreatedAt = transfer.CreatedAt
            };

            _context.Transfers.Add(record);
        }

        await _context.SaveChangesAsync(ct);
    }
}
