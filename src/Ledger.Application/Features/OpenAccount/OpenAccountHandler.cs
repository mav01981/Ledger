using Ledger.Application.Shared;
using MediatR;
using Ledger.Domain;
using Ledger.Domain.Events;

namespace Ledger.Application.Features.OpenAccount;

public class OpenAccountHandler : IRequestHandler<OpenAccountCommand, CommandResult>
{
    private readonly IEventStore _eventStore;
    private readonly IIdempotencyService _idempotency;
    private readonly TimeProvider _timeProvider;

    public OpenAccountHandler(IEventStore eventStore, IIdempotencyService idempotency, TimeProvider timeProvider)
    {
        _eventStore = eventStore;
        _idempotency = idempotency;
        _timeProvider = timeProvider;
    }

    public async Task<CommandResult> Handle(OpenAccountCommand request, CancellationToken ct)
    {
        if (await _idempotency.HasBeenProcessedAsync(request.IdempotencyKey, ct))
        {
            return CommandResult.Fail("Duplicate command.");
        }
        
        var account = Account.Open(request.AccountId, request.AccountType, _timeProvider.GetUtcNow().UtcDateTime);
        var events = account.UncommittedEvents;

        // Event store writes events + outbox atomically
        await _eventStore.AppendToStreamAsync(request.AccountId, events, expectedVersion: -1, ct);
        await _idempotency.MarkProcessedAsync(request.IdempotencyKey, request.AccountId, ct);

        return CommandResult.Ok(request.AccountId, account.Version);
    }
}
