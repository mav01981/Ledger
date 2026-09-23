using MediatR;
using Ledger.Domain.Models;
using Ledger.Domain;

namespace Ledger.Application.Features.GetAccountBalance;

public class GetAccountBalanceHandler : IRequestHandler<GetAccountBalanceQuery, AccountBalanceDto?>
{
    private readonly IReadModel _readModel;
    public GetAccountBalanceHandler(IReadModel readModel) => _readModel = readModel;
    public Task<AccountBalanceDto?> Handle(GetAccountBalanceQuery request, CancellationToken ct)
        => _readModel.GetAccountBalanceAsync(request.AccountId, ct);
}
