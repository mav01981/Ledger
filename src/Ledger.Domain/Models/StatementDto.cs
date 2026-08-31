namespace Ledger.Domain.Models;

public record StatementDto(
    Guid AccountId,
    DateTime PeriodStart,
    DateTime PeriodEnd,
    decimal OpeningBalance,
    decimal ClosingBalance,
    decimal TotalCredits,
    decimal TotalDebits,
    IReadOnlyList<TransactionEntryDto> Transactions
);
