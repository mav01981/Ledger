using Ledger.Application.Shared;
using Ledger.Domain;
using Ledger.Domain.Events;
using MediatR;

namespace Ledger.Application.Features.Withdraw;

public class WithdrawHandler : IRequestHandler<WithdrawCommand, CommandResult>
{
    private readonly IEventStore _eventStore;
    private readonly IIdempotencyService _idempotency;
    private readonly TimeProvider _timeProvider;

    public WithdrawHandler(IEventStore eventStore, IIdempotencyService idempotency, TimeProvider timeProvider)
    {
        _eventStore = eventStore;
        _idempotency = idempotency;
        _timeProvider = timeProvider;
    }

    public async Task<CommandResult> Handle(WithdrawCommand request, CancellationToken ct)
    {
        if (await _idempotency.HasBeenProcessedAsync(request.IdempotencyKey, ct))
            return CommandResult.Fail("Duplicate command.");

        var account = await LoadAccountAsync(request.AccountId, ct);

        // Check if account exists
        if (account.Version < 0)
        {
            return CommandResult.Fail("Account not found.");
        }

        var txId = Guid.NewGuid();

        try
        {
            account.Withdraw(txId, new Money(request.Amount), _timeProvider.GetUtcNow().UtcDateTime);
        }
        catch (DomainException ex)
        {
            return CommandResult.Fail(ex.Message);
        }

        var events = account.UncommittedEvents;

        await _eventStore.AppendToStreamAsync(request.AccountId, events, account.Version - events.Count, ct);
        await _idempotency.MarkProcessedAsync(request.IdempotencyKey, request.AccountId, ct);

        return CommandResult.Ok(request.AccountId, account.Version);
    }

    private async Task<Account> LoadAccountAsync(Guid id, CancellationToken ct)
    {
        var events = await _eventStore.ReadStreamAsync(id, ct: ct);
        var account = new Account();
        account.LoadFromHistory(events);
        return account;
    }
}
