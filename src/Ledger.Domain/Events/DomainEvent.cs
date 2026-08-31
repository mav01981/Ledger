namespace Ledger.Domain.Events;

public abstract record DomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public long StreamPosition { get; init; }
}

public record AccountOpened(
    Guid AccountId,
    AccountType AccountType
) : DomainEvent;

public record FundsDeposited(
    Guid AccountId,
    Guid TransactionId,
    Money Amount
) : DomainEvent
{
    public static FundsDeposited Create(Guid accountId, Guid transactionId, Money amount, DateTime timestamp)
        => new(accountId, transactionId, amount) { Timestamp = timestamp };
}

public record FundsWithdrawn(
    Guid AccountId,
    Guid TransactionId,
    Money Amount
) : DomainEvent
{
    public static FundsWithdrawn Create(Guid accountId, Guid transactionId, Money amount, DateTime timestamp)
        => new(accountId, transactionId, amount) { Timestamp = timestamp };
}

public record TransferPosted(
    Guid TransactionId,
    IReadOnlyList<TransactionLine> Lines
) : DomainEvent;

public record TransactionReversed(
    Guid OriginalTransactionId,
    Guid ReversalTransactionId
) : DomainEvent;
