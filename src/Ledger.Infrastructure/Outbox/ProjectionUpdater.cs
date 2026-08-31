using Ledger.Domain;
using Ledger.Domain.Events;
using Ledger.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Ledger.Infrastructure.Outbox;

public class ProjectionUpdater
{
    private readonly LedgerDbContext _context;

    public ProjectionUpdater(LedgerDbContext context)
    {
        _context = context;
    }

    public async Task UpdateAsync(DomainEvent @event, CancellationToken ct)
    {
        switch (@event)
        {
            case AccountOpened e:
                await ApplyAccountOpenedAsync(e, ct);
                break;
            case FundsDeposited e:
                await ApplyFundsDepositedAsync(e, ct);
                break;
            case FundsWithdrawn e:
                await ApplyFundsWithdrawnAsync(e, ct);
                break;
            case TransferPosted e:
                await ApplyTransferPostedAsync(e, ct);
                break;
            case TransactionReversed e:
                await ApplyTransactionReversedAsync(e, ct);
                break;
        }
    }

    private async Task ApplyAccountOpenedAsync(AccountOpened e, CancellationToken ct)
    {
        var existing = await _context.AccountBalances
            .FirstOrDefaultAsync(a => a.AccountId == e.AccountId, ct);

        if (existing == null)
        {
            _context.AccountBalances.Add(new AccountBalanceReadModel
            {
                AccountId = e.AccountId,
                AccountType = e.AccountType.ToString(),
                Balance = 0m,
                Status = AccountStatus.Open.ToString(),
                OpenedAt = e.Timestamp,
                LastEventVersion = 0
            });
        }

        // Add account opening to transaction history
        _context.TransactionHistory.Add(new TransactionHistoryReadModel
        {
            AccountId = e.AccountId,
            TransactionId = e.AccountId, // Use account ID as the "opening" transaction ID
            Amount = 0m,
            Direction = "Credit",
            Timestamp = e.Timestamp,
            RunningBalance = 0m
        });

        await _context.SaveChangesAsync(ct);
    }

    private async Task ApplyFundsDepositedAsync(FundsDeposited e, CancellationToken ct)
    {
        var account = await _context.AccountBalances
            .FirstOrDefaultAsync(a => a.AccountId == e.AccountId, ct);

        if (account != null)
        {
            account.Balance += e.Amount.Amount;
            account.LastEventVersion++;
        }

        _context.TransactionHistory.Add(new TransactionHistoryReadModel
        {
            AccountId = e.AccountId,
            TransactionId = e.TransactionId,
            Amount = e.Amount.Amount,
            Direction = "Credit",
            Timestamp = e.Timestamp,
            RunningBalance = account?.Balance ?? e.Amount.Amount
        });

        await _context.SaveChangesAsync(ct);
    }

    private async Task ApplyFundsWithdrawnAsync(FundsWithdrawn e, CancellationToken ct)
    {
        var account = await _context.AccountBalances
            .FirstOrDefaultAsync(a => a.AccountId == e.AccountId, ct);

        if (account is not null)
        {
            account.Balance -= e.Amount.Amount;
            account.LastEventVersion++;
        }

        _context.TransactionHistory.Add(new TransactionHistoryReadModel
        {
            AccountId = e.AccountId,
            TransactionId = e.TransactionId,
            Amount = e.Amount.Amount,
            Direction = "Debit",
            Timestamp = e.Timestamp,
            RunningBalance = account?.Balance ?? -e.Amount.Amount
        });

        await _context.SaveChangesAsync(ct);
    }

    private async Task ApplyTransferPostedAsync(TransferPosted ev, CancellationToken ct)
    {
        foreach (var line in ev.Lines)
        {
            var account = await _context.AccountBalances
                .FirstOrDefaultAsync(a => a.AccountId == line.AccountId, ct);

            if (account is not null)
            {
                var amount = line.Amount.Amount;
                if (line.Direction == DebitCredit.Debit)
                    account.Balance -= amount;
                else
                    account.Balance += amount;
                account.LastEventVersion++;
            }

            _context.TransactionHistory.Add(new TransactionHistoryReadModel
            {
                AccountId = line.AccountId,
                TransactionId = ev.TransactionId,
                Amount = line.Amount.Amount,
                Direction = line.Direction == DebitCredit.Debit ? "Debit" : "Credit",
                Timestamp = ev.Timestamp,
                RunningBalance = account?.Balance ?? 0m
            });
        }
        await _context.SaveChangesAsync(ct);
    }

    private static async Task ApplyTransactionReversedAsync(TransactionReversed e, CancellationToken ct)
    {
        // Reversal events don't directly update balances - the offsetting debits/credits do
        // This is just for audit trail in the projection
        await Task.CompletedTask;
    }
}
