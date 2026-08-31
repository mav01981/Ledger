namespace Ledger.Domain;

public enum DebitCredit
{
    Debit = 0,
    Credit = 1
}

public sealed record TransactionLine(
    Guid AccountId,
    Money Amount,
    DebitCredit Direction
);
