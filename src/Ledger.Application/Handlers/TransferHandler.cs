using MediatR;
using Ledger.Application.Commands;
using Ledger.Domain;
using Ledger.Domain.Events;

namespace Ledger.Application.Handlers;

public class TransferHandler : IRequestHandler<TransferCommand, CommandResult>
{
    private readonly IEventStore _eventStore;
    private readonly IIdempotencyService _idempotency;
    private readonly ITransactionStore _transactionStore;
    private readonly TimeProvider _timeProvider;

    public TransferHandler(IEventStore eventStore, IIdempotencyService idempotency, ITransactionStore transactionStore, TimeProvider timeProvider)
    {
        _eventStore = eventStore;
        _idempotency = idempotency;
        _transactionStore = transactionStore;
        _timeProvider = timeProvider;
    }

    public async Task<CommandResult> Handle(TransferCommand request, CancellationToken ct)
    {
        if (await _idempotency.HasBeenProcessedAsync(request.IdempotencyKey, ct))
            return CommandResult.Fail("Duplicate command.");

        var fromAccount = await LoadAccountAsync(request.FromAccountId, ct);
        var toAccount = await LoadAccountAsync(request.ToAccountId, ct);

        // Check if accounts exist
        if (fromAccount.Version < 0)
            return CommandResult.Fail("Source account not found.");
        if (toAccount.Version < 0)
            return CommandResult.Fail("Destination account not found.");

        var transferId = Guid.NewGuid();
        var amount = new Money(request.Amount);

        try
        {
            fromAccount.ApplyDebit(transferId, amount, _timeProvider.GetUtcNow().UtcDateTime);
            toAccount.ApplyCredit(transferId, amount, _timeProvider.GetUtcNow().UtcDateTime);
        }
        catch (DomainException ex)
        {
            return CommandResult.Fail(ex.Message);
        }

        var fromEvents = fromAccount.UncommittedEvents;
        var toEvents = toAccount.UncommittedEvents;

        await _eventStore.AppendToStreamAsync(request.FromAccountId, fromEvents, fromAccount.Version - fromEvents.Count, ct);
        await _eventStore.AppendToStreamAsync(request.ToAccountId, toEvents, toAccount.Version - toEvents.Count, ct);

        // Record the transfer for reversal support
        var transfer = new Transfer
        {
            TransactionId = transferId,
            Lines =
            [
                new(request.FromAccountId, amount, DebitCredit.Debit),
                new(request.ToAccountId, amount, DebitCredit.Credit)
            ],
            Status = TransferStatus.Posted,
            CreatedAt = _timeProvider.GetUtcNow().UtcDateTime
        };
        await _transactionStore.SaveAsync(transfer, ct);

        await _idempotency.MarkProcessedAsync(request.IdempotencyKey, request.FromAccountId, ct);

        return CommandResult.Ok(request.FromAccountId, fromAccount.Version);
    }

    private async Task<Account> LoadAccountAsync(Guid id, CancellationToken ct)
    {
        var events = await _eventStore.ReadStreamAsync(id, ct: ct);
        var account = new Account();
        account.LoadFromHistory(events);
        return account;
    }
}
