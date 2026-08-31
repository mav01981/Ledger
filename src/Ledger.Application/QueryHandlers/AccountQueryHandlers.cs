using MediatR;
using Ledger.Domain;
using Ledger.Domain.Models;
using Ledger.Application.Queries;

namespace Ledger.Application.QueryHandlers;

public class GetAccountBalanceHandler : IRequestHandler<GetAccountBalanceQuery, AccountBalanceDto?>
{
    private readonly IReadModel _readModel;
    public GetAccountBalanceHandler(IReadModel readModel) => _readModel = readModel;
    public Task<AccountBalanceDto?> Handle(GetAccountBalanceQuery request, CancellationToken ct)
        => _readModel.GetAccountBalanceAsync(request.AccountId, ct);
}

public class GetAllAccountsHandler : IRequestHandler<GetAllAccountsQuery, IReadOnlyList<AccountBalanceDto>>
{
    private readonly IReadModel _readModel;
    public GetAllAccountsHandler(IReadModel readModel) => _readModel = readModel;
    public Task<IReadOnlyList<AccountBalanceDto>> Handle(GetAllAccountsQuery request, CancellationToken ct)
        => _readModel.GetAllAccountsAsync(ct);
}

public class GetTransactionHistoryHandler : IRequestHandler<GetTransactionHistoryQuery, TransactionHistoryDto?>
{
    private readonly IReadModel _readModel;
    public GetTransactionHistoryHandler(IReadModel readModel) => _readModel = readModel;
    public Task<TransactionHistoryDto?> Handle(GetTransactionHistoryQuery request, CancellationToken ct)
        => _readModel.GetTransactionHistoryAsync(request.AccountId, ct);
}

public class GetStatementHandler : IRequestHandler<GetStatementQuery, StatementDto?>
{
    private readonly IReadModel _readModel;
    public GetStatementHandler(IReadModel readModel) => _readModel = readModel;
    public Task<StatementDto?> Handle(GetStatementQuery request, CancellationToken ct)
        => _readModel.GetStatementAsync(request.AccountId, request.PeriodStart, request.PeriodEnd, ct);
}

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

        var account = new Account();
        if (filtered.Count > 0)
        {
            account.LoadFromHistory(filtered);
        }

        return account.Balance.Amount;
    }
}
