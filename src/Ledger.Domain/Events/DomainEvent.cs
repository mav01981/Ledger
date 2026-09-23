namespace Ledger.Domain.Events;

public abstract record DomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();

    /// <summary>
    /// The instant the event happened. Required, so no event can silently fall back on the wall clock,
    /// and normalized to UTC, because the value is persisted to a timestamptz column and replayed for
    /// point-in-time queries - an ambiguous instant there would corrupt history.
    /// </summary>
    public required DateTime Timestamp
    {
        get => field;
        init => field = value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => throw new ArgumentException(
                "Event timestamps must be UTC or local time; Unspecified does not identify an instant.",
                nameof(value))
        };
    }

    public long StreamPosition { get; init; }
}

public record AccountOpened(
    Guid AccountId,
    AccountType AccountType
) : DomainEvent
{
    public static AccountOpened Create(Guid accountId, AccountType accountType, DateTime timestamp)
        => new(accountId, accountType) { Timestamp = timestamp };
}

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
