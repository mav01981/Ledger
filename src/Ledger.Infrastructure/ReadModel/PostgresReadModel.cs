using Ledger.Domain;
using Ledger.Domain.Models;
using Ledger.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Ledger.Infrastructure.ReadModel;

public class PostgresReadModel : IReadModel
{
    // Explicit bound: a read endpoint must never pull an unbounded result set into memory.
    // Replace with real page/pageSize paging (on the query, handler, and endpoint together) once clients need it.
    private const int MaxAccountsReturned = 500;

    private const string CreditDirection = "Credit";
    private const string DebitDirection = "Debit";

    private readonly LedgerDbContext _context;
    private readonly TimeProvider _timeProvider;

    public PostgresReadModel(LedgerDbContext context, TimeProvider timeProvider)
    {
        _context = context;
        _timeProvider = timeProvider;
    }

    public Task<AccountBalanceDto?> GetAccountBalanceAsync(Guid accountId, CancellationToken ct = default)
        => _context.AccountBalances
            .AsNoTracking()
            .Where(a => a.AccountId == accountId)
            .Select(a => new AccountBalanceDto(
                a.AccountId,
                a.AccountType,
                a.Balance,
                a.Status,
                a.OpenedAt,
                a.LastEventVersion))
            .FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<AccountBalanceDto>> GetAllAccountsAsync(CancellationToken ct = default)
        => await _context.AccountBalances
            .AsNoTracking()
            .OrderBy(a => a.OpenedAt)
            .ThenBy(a => a.AccountId)
            .Take(MaxAccountsReturned)
            .Select(a => new AccountBalanceDto(
                a.AccountId,
                a.AccountType,
                a.Balance,
                a.Status,
                a.OpenedAt,
                a.LastEventVersion))
            .ToListAsync(ct);

    public async Task<TransactionHistoryDto?> GetTransactionHistoryAsync(Guid accountId, CancellationToken ct = default)
    {
        var entries = await ReadEntriesAsync(accountId, from: null, to: null, ct);

        return entries.Count == 0 ? null : new TransactionHistoryDto(accountId, entries);
    }

    public async Task<StatementDto?> GetStatementAsync(Guid accountId, DateTime? periodStart, DateTime? periodEnd, CancellationToken ct = default)
    {
        var start = AsUtc(periodStart) ?? FirstDayOfCurrentMonthUtc();
        var end = AsUtc(periodEnd) ?? start.AddMonths(1);

        var entries = await ReadEntriesAsync(accountId, start, end, ct);

        if (entries.Count == 0) return null;

        return new StatementDto(
            accountId,
            start,
            end,
            OpeningBalance(entries[0]),
            entries[^1].RunningBalance,
            TotalForDirection(entries, CreditDirection),
            TotalForDirection(entries, DebitDirection),
            entries);
    }

    private Task<List<TransactionEntryDto>> ReadEntriesAsync(Guid accountId, DateTime? from, DateTime? to, CancellationToken ct)
        => _context.TransactionHistory
            .AsNoTracking()
            .Where(t => t.AccountId == accountId)
            .Where(t => from == null || t.Timestamp >= from)
            .Where(t => to == null || t.Timestamp < to)
            .OrderBy(t => t.Timestamp)
            .Select(t => new TransactionEntryDto(
                t.TransactionId,
                t.Timestamp,
                t.Amount,
                t.Direction,
                t.RunningBalance))
            .ToListAsync(ct);

    private DateTime FirstDayOfCurrentMonthUtc()
    {
        // Read the clock once: Year and Month can never come from different months across a month boundary.
        var now = _timeProvider.GetUtcNow();

        // Kind must be Utc: Npgsql rejects Unspecified/Local DateTimes for timestamp with time zone parameters.
        return new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
    }

    private static DateTime? AsUtc(DateTime? value) => value switch
    {
        null => null,
        { Kind: DateTimeKind.Utc } utc => utc,
        { Kind: DateTimeKind.Local } local => local.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value.Value, DateTimeKind.Utc)
    };

    private static decimal OpeningBalance(TransactionEntryDto first) => first.Direction switch
    {
        CreditDirection => first.RunningBalance - first.Amount,
        DebitDirection => first.RunningBalance + first.Amount,
        _ => 0m
    };

    private static decimal TotalForDirection(IEnumerable<TransactionEntryDto> entries, string direction)
        => entries.Where(e => e.Direction == direction).Sum(e => e.Amount);
}
