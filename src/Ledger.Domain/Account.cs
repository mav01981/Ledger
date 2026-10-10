using Ledger.Domain.Events;

namespace Ledger.Domain;

public class Account : AggregateRoot
{
    public AccountType AccountType { get; private set; }
    public AccountStatus Status { get; private set; }
    public Money Balance { get; private set; } = Money.Zero;
    public DateTime OpenedAt { get; private set; }

    // For rehydration
    public Account() { }

    public static Account Open(Guid accountId, AccountType accountType, DateTime occurredAt)
    {
        var account = new Account();
        account.Apply(AccountOpened.Create(accountId, accountType, occurredAt));
        return account;
    }

    public void Deposit(Guid transactionId, Money amount, DateTime occurredAt)
    {
        EnsureOpen();
        EnsurePositiveAmount(amount, "Deposit");

        Apply(FundsDeposited.Create(Id, transactionId, amount, occurredAt));
    }

    public void Withdraw(Guid transactionId, Money amount, DateTime occurredAt)
    {
        EnsureOpen();
        EnsurePositiveAmount(amount, "Withdrawal");

        var newBalance = Balance - amount;
        if (newBalance.IsNegative && AccountType != AccountType.Overdraft)
            throw new DomainException("Insufficient funds.");

        Apply(FundsWithdrawn.Create(Id, transactionId, amount, occurredAt));
    }

    public void ApplyDebit(Guid transactionId, Money amount, DateTime occurredAt)
    {
        EnsureOpen();
        EnsurePositiveAmount(amount, "Debit");

        var newBalance = Balance - amount;
        if (newBalance.IsNegative && AccountType != AccountType.Overdraft)
            throw new DomainException($"Insufficient funds in account {Id}.");

        Apply(FundsWithdrawn.Create(Id, transactionId, amount, occurredAt));
    }

    public void ApplyCredit(Guid transactionId, Money amount, DateTime occurredAt)
    {
        EnsureOpen();
        EnsurePositiveAmount(amount, "Credit");

        Apply(FundsDeposited.Create(Id, transactionId, amount, occurredAt));
    }

    private static void EnsurePositiveAmount(Money amount, string operation)
    {
        if (amount.IsZero || amount.IsNegative)
            throw new DomainException($"{operation} amount must be positive.");
    }

    private void EnsureOpen()
    {
        if (Status != AccountStatus.Open)
            throw new DomainException("Account is not open.");
    }

    protected override void When(DomainEvent @event)
    {
        switch (@event)
        {
            case AccountOpened e:
                Id = e.AccountId;
                AccountType = e.AccountType;
                Status = AccountStatus.Open;
                Balance = Money.Zero;
                OpenedAt = e.Timestamp;
                break;
            case FundsDeposited e:
                Balance += e.Amount;
                break;
            case FundsWithdrawn e:
                Balance -= e.Amount;
                break;
        }
    }
}
