using Ledger.Application.Shared;
using Ledger.Domain;
using Ledger.Domain.Events;
using MediatR;

namespace Ledger.Application.Features.Reverse;

public class ReverseTransactionHandler : IRequestHandler<ReverseTransactionCommand, CommandResult>
{
    private readonly IEventStore _eventStore;
    private readonly IIdempotencyService _idempotency;
    private readonly ITransactionStore _transactionStore;
    private readonly TimeProvider _timeProvider;

    public ReverseTransactionHandler(IEventStore eventStore, IIdempotencyService idempotency, ITransactionStore transactionStore, TimeProvider timeProvider)
    {
        _eventStore = eventStore;
        _idempotency = idempotency;
        _transactionStore = transactionStore;
        _timeProvider = timeProvider;
    }

    public async Task<CommandResult> Handle(ReverseTransactionCommand request, CancellationToken ct)
    {
        if (await _idempotency.HasBeenProcessedAsync(request.IdempotencyKey, ct))
            return CommandResult.Fail("Duplicate command.");

        var transfer = await _transactionStore.GetByIdAsync(request.TransactionId, ct);
        if (transfer == null)
            return CommandResult.Fail("Transaction not found.");

        if (transfer.Status != TransferStatus.Posted)
            return CommandResult.Fail("Transaction already reversed.");

        var reversalId = Guid.NewGuid();
        long finalVersion = -1;

        try
        {
            foreach (var line in transfer.Lines)
            {
                var account = await LoadAccountAsync(line.AccountId, ct);

                // Check if account exists
                if (account.Version < 0)
                    return CommandResult.Fail("Account not found.");

                var oppositeAmount = line.Amount;

                if (line.Direction == DebitCredit.Debit)
                {
                    account.ApplyCredit(reversalId, oppositeAmount, _timeProvider.GetUtcNow().UtcDateTime);
                }
                else
                {
                    account.ApplyDebit(reversalId, oppositeAmount, _timeProvider.GetUtcNow().UtcDateTime);
                }

                var events = account.UncommittedEvents;
                await _eventStore.AppendToStreamAsync(line.AccountId, events, account.Version - events.Count, ct);
                finalVersion = account.Version;
            }
        }
        catch (DomainException ex)
        {
            return CommandResult.Fail(ex.Message);
        }

        transfer.Status = TransferStatus.Reversed;
        await _transactionStore.SaveAsync(transfer, ct);

        await _idempotency.MarkProcessedAsync(request.IdempotencyKey, transfer.TransactionId, ct);

        return CommandResult.Ok(transfer.TransactionId, finalVersion);
    }

    private async Task<Account> LoadAccountAsync(Guid id, CancellationToken ct)
    {
        var events = await _eventStore.ReadStreamAsync(id, ct: ct);
        var account = new Account();
        account.LoadFromHistory(events);
        return account;
    }
}
