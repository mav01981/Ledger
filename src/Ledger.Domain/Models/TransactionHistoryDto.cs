namespace Ledger.Domain.Models;

public record TransactionHistoryDto(
    Guid AccountId,
    IReadOnlyList<TransactionEntryDto> Transactions
);

public record TransactionEntryDto(
    Guid TransactionId,
    DateTime Timestamp,
    decimal Amount,
    string Direction,
    decimal RunningBalance
);
