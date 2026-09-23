using MediatR;
using Ledger.Domain.Models;
using Ledger.Domain;

namespace Ledger.Application.Features.GetAllAccounts;

public class GetAllAccountsHandler : IRequestHandler<GetAllAccountsQuery, IReadOnlyList<AccountBalanceDto>>
{
    private readonly IReadModel _readModel;
    public GetAllAccountsHandler(IReadModel readModel) => _readModel = readModel;
    public Task<IReadOnlyList<AccountBalanceDto>> Handle(GetAllAccountsQuery request, CancellationToken ct)
        => _readModel.GetAllAccountsAsync(ct);
}
