using MediatR;
using Ledger.Application.Commands;
using Ledger.Domain;
using Ledger.Domain.Events;

namespace Ledger.Application.Handlers;

public class OpenAccountHandler : IRequestHandler<OpenAccountCommand, CommandResult>
{
    private readonly IEventStore _eventStore;
    private readonly IIdempotencyService _idempotency;

    public OpenAccountHandler(IEventStore eventStore, IIdempotencyService idempotency)
    {
        _eventStore = eventStore;
        _idempotency = idempotency;
    }

    public async Task<CommandResult> Handle(OpenAccountCommand request, CancellationToken ct)
    {
        if (await _idempotency.HasBeenProcessedAsync(request.IdempotencyKey, ct))
        {
            return CommandResult.Fail("Duplicate command.");
        }
        
        var account = Account.Open(request.AccountId, request.AccountType);
        var events = account.UncommittedEvents;

        // Event store writes events + outbox atomically
        await _eventStore.AppendToStreamAsync(request.AccountId, events, expectedVersion: -1, ct);
        await _idempotency.MarkProcessedAsync(request.IdempotencyKey, request.AccountId, ct);

        return CommandResult.Ok(request.AccountId, account.Version);
    }
}
