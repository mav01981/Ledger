namespace Ledger.Domain.Models;

public record AccountBalanceDto(
    Guid AccountId,
    string AccountType,
    decimal Balance,
    string Status,
    DateTime OpenedAt,
    long LastEventVersion
);
