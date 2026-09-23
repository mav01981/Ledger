using Ledger.Domain;
using MediatR;

namespace Ledger.Application.Features.GetPointInTimeBalance;

public class GetPointInTimeBalanceHandler : IRequestHandler<GetPointInTimeBalanceQuery, decimal>
{
    private readonly IEventStore _eventStore;

    public GetPointInTimeBalanceHandler(IEventStore eventStore)
    {
        _eventStore = eventStore;
    }

    public async Task<decimal> Handle(GetPointInTimeBalanceQuery request, CancellationToken ct)
    {
        // Point-in-time: replay events up to the given timestamp
        var events = await _eventStore.ReadStreamAsync(request.AccountId, ct: ct);
        
        // Ensure AsOf is UTC for consistent comparison
        var asOf = request.AsOf.Kind == DateTimeKind.Unspecified 
            ? DateTime.SpecifyKind(request.AsOf, DateTimeKind.Utc) 
            : request.AsOf.ToUniversalTime();
        
        var filtered = events.Where(e =>
        {
            var eventTimestamp = e.Timestamp.Kind == DateTimeKind.Unspecified 
                ? DateTime.SpecifyKind(e.Timestamp, DateTimeKind.Utc) 
                : e.Timestamp.ToUniversalTime();
            return eventTimestamp <= asOf;
        }).ToList();

        var account = new Domain.Account();
        if (filtered.Count > 0)
        {
            account.LoadFromHistory(filtered);
        }

        return account.Balance.Amount;
    }
}
