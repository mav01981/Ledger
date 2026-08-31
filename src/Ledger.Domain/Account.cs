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

    public static Account Open(Guid accountId, AccountType accountType)
    {
        var account = new Account();
        account.Apply(new AccountOpened(accountId, accountType) { Timestamp = DateTime.UtcNow });
        return account;
    }

    public void Deposit(Guid transactionId, Money amount)
    {
        EnsureOpen();
        if (amount.IsZero || amount.IsNegative)
            throw new DomainException("Deposit amount must be positive.");

        Apply(FundsDeposited.Create(Id, transactionId, amount, DateTime.UtcNow));
    }

    public void Withdraw(Guid transactionId, Money amount)
    {
        EnsureOpen();
        if (amount.IsZero || amount.IsNegative)
            throw new DomainException("Withdrawal amount must be positive.");

        var newBalance = Balance - amount;
        if (newBalance.IsNegative && AccountType != AccountType.Overdraft)
            throw new DomainException("Insufficient funds.");

        Apply(FundsWithdrawn.Create(Id, transactionId, amount, DateTime.UtcNow));
    }

    public void ApplyDebit(Guid transactionId, Money amount)
    {
        EnsureOpen();
        var newBalance = Balance - amount;
        if (newBalance.IsNegative && AccountType != AccountType.Overdraft)
            throw new DomainException($"Insufficient funds in account {Id}.");

        Apply(FundsWithdrawn.Create(Id, transactionId, amount, DateTime.UtcNow));
    }

    public void ApplyCredit(Guid transactionId, Money amount)
    {
        EnsureOpen();
        Apply(FundsDeposited.Create(Id, transactionId, amount, DateTime.UtcNow));
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
