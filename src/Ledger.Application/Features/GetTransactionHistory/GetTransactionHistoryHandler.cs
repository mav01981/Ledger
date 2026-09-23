using MediatR;
using Ledger.Domain.Models;
using Ledger.Domain;

namespace Ledger.Application.Features.GetTransactionHistory;

public class GetTransactionHistoryHandler : IRequestHandler<GetTransactionHistoryQuery, TransactionHistoryDto?>
{
    private readonly IReadModel _readModel;
    public GetTransactionHistoryHandler(IReadModel readModel) => _readModel = readModel;
    public Task<TransactionHistoryDto?> Handle(GetTransactionHistoryQuery request, CancellationToken ct)
        => _readModel.GetTransactionHistoryAsync(request.AccountId, ct);
}
