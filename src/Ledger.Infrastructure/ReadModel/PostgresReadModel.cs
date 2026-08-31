using Ledger.Domain;
using Ledger.Domain.Models;
using Ledger.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Ledger.Infrastructure.ReadModel;

public class PostgresReadModel : IReadModel
{
    private readonly LedgerDbContext _context;

    public PostgresReadModel(LedgerDbContext context)
    {
        _context = context;
    }

    public async Task<AccountBalanceDto?> GetAccountBalanceAsync(Guid accountId, CancellationToken ct = default)
    {
        var record = await _context.AccountBalances
            .FirstOrDefaultAsync(a => a.AccountId == accountId, ct);

        if (record == null) return null;

        return new AccountBalanceDto(
            record.AccountId,
            record.AccountType,
            record.Balance,
            record.Status,
            record.OpenedAt,
            record.LastEventVersion
        );
    }

    public async Task<IReadOnlyList<AccountBalanceDto>> GetAllAccountsAsync(CancellationToken ct = default)
    {
        var records = await _context.AccountBalances.ToListAsync(ct);
        return records.Select(r => new AccountBalanceDto(
            r.AccountId,
            r.AccountType,
            r.Balance,
            r.Status,
            r.OpenedAt,
            r.LastEventVersion
        )).ToList();
    }

    public async Task<TransactionHistoryDto?> GetTransactionHistoryAsync(Guid accountId, CancellationToken ct = default)
    {
        var records = await _context.TransactionHistory
            .Where(t => t.AccountId == accountId)
            .OrderBy(t => t.Timestamp)
            .ToListAsync(ct);

        if (records.Count == 0) return null;

        var entries = records.Select(r => new TransactionEntryDto(
            r.TransactionId,
            r.Timestamp,
            r.Amount,
            r.Direction,
            r.RunningBalance
        )).ToList();

        return new TransactionHistoryDto(accountId, entries);
    }

    public async Task<StatementDto?> GetStatementAsync(Guid accountId, DateTime? periodStart, DateTime? periodEnd, CancellationToken ct = default)
    {
        var start = periodStart ?? new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
        var end = periodEnd ?? start.AddMonths(1);

        var records = await _context.TransactionHistory
            .Where(t => t.AccountId == accountId && t.Timestamp >= start && t.Timestamp < end)
            .OrderBy(t => t.Timestamp)
            .ToListAsync(ct);

        if (records.Count == 0) return null;

        var entries = records.Select(r => new TransactionEntryDto(
            r.TransactionId,
            r.Timestamp,
            r.Amount,
            r.Direction,
            r.RunningBalance
        )).ToList();

        var openingBalance = entries.Count > 0 && entries.First().Direction == "Credit"
            ? entries.First().RunningBalance - entries.First().Amount
            : entries.Count > 0 && entries.First().Direction == "Debit"
                ? entries.First().RunningBalance + entries.First().Amount
                : 0m;

        var closingBalance = entries.Count > 0 ? entries.Last().RunningBalance : 0m;
        var totalCredits = entries.Where(e => e.Direction == "Credit").Sum(e => e.Amount);
        var totalDebits = entries.Where(e => e.Direction == "Debit").Sum(e => e.Amount);

        return new StatementDto(
            accountId,
            start,
            end,
            openingBalance,
            closingBalance,
            totalCredits,
            totalDebits,
            entries
        );
    }
}
