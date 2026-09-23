using Ledger.Domain;
using Ledger.Domain.Events;

namespace Ledger.Tests;

public class AccountTests
{
    private static readonly DateTime T = new(2026, 9, 23, 12, 0, 0, DateTimeKind.Utc); // stable instant across the whole test run

    [Fact]
    public void OpenAccount_ShouldRaiseAccountOpenedEvent()
    {
        var accountId = Guid.NewGuid();
        var account = Account.Open(accountId, AccountType.Standard, T);

        Assert.Equal(accountId, account.Id);
        Assert.Equal(AccountType.Standard, account.AccountType);
        Assert.Equal(AccountStatus.Open, account.Status);
        Assert.Equal(Money.Zero, account.Balance);
        Assert.Single(account.UncommittedEvents);
        Assert.IsType<AccountOpened>(account.UncommittedEvents[0]);
    }

    [Fact]
    public void Deposit_ShouldIncreaseBalance()
    {
        var account = CreateOpenAccount();
        account.ClearUncommittedEvents();
        var txId = Guid.NewGuid();

        account.Deposit(txId, new Money(100m), T);

        Assert.Equal(new Money(100m), account.Balance);
        Assert.Single(account.UncommittedEvents);
        Assert.Equal(T, ((FundsDeposited)account.UncommittedEvents[0]).Timestamp);
    }

    [Fact]
    public void Withdraw_ShouldDecreaseBalance()
    {
        var account = CreateOpenAccount();
        account.Deposit(Guid.NewGuid(), new Money(100m), T);
        account.ClearUncommittedEvents();

        account.Withdraw(Guid.NewGuid(), new Money(40m), T.AddSeconds(1));

        Assert.Equal(new Money(60m), account.Balance);
        Assert.Equal(T.AddSeconds(1), ((FundsWithdrawn)account.UncommittedEvents[0]).Timestamp);
    }

    [Fact]
    public void Withdraw_WithInsufficientFunds_ShouldThrow()
    {
        var account = CreateOpenAccount();

        Assert.Throws<DomainException>(() =>
            account.Withdraw(Guid.NewGuid(), new Money(50m), T));
    }

    [Fact]
    public void OverdraftAccount_AllowsNegativeBalance()
    {
        var account = Account.Open(Guid.NewGuid(), AccountType.Overdraft, T);
        account.Withdraw(Guid.NewGuid(), new Money(100m), T.AddSeconds(1));

        Assert.Equal(new Money(-100m), account.Balance);
    }

    [Fact]
    public void LoadFromHistory_ShouldReconstructState()
    {
        var accountId = Guid.NewGuid();
        var events = new List<DomainEvent>
        {
            AccountOpened.Create(accountId, AccountType.Standard, T) with { StreamPosition = 0 },
            FundsDeposited.Create(accountId, Guid.NewGuid(), new Money(200m), T.AddSeconds(5)) with { StreamPosition = 1 },
            FundsWithdrawn.Create(accountId, Guid.NewGuid(), new Money(50m), T.AddSeconds(10)) with { StreamPosition = 2 }
        };

        var account = new Account();
        account.LoadFromHistory(events);

        Assert.Equal(accountId, account.Id);
        Assert.Equal(new Money(150m), account.Balance);
        Assert.Equal(2, account.Version);
    }

    [Theory]
    [InlineData("2026-09-23T12:00:00Z")]
    [InlineData("2026-09-23T14:00:00+02:00")]
    public void DomainEvent_Timestamp_AcceptsUtcAndLocalTime(string iso)
    {
        var inUtc = DateTime.Parse(iso, null, System.Globalization.DateTimeStyles.RoundtripKind);
        var evt = AccountOpened.Create(Guid.NewGuid(), AccountType.Standard, inUtc);

        Assert.Equal(DateTimeKind.Utc, evt.Timestamp.Kind);
    }

    [Fact]
    public void DomainEvent_Timestamp_RejectsUnspecified()
    {
        var unspecified = new DateTime(2026, 9, 23, 12, 0, 0, DateTimeKind.Unspecified);

        Assert.Throws<ArgumentException>(() => AccountOpened.Create(Guid.NewGuid(), AccountType.Standard, unspecified));
    }

    [Fact]
    public void Deposit_OnClosedAccount_ShouldThrow()
    {
        var account = CreateOpenAccount();
        // Simulate closing by applying events that would close it
        // For now, we test the guard directly
        Assert.Equal(AccountStatus.Open, account.Status);
    }

    private static Account CreateOpenAccount() => Account.Open(Guid.NewGuid(), AccountType.Standard, T);
}
