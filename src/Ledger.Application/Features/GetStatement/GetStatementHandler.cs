using MediatR;
using Ledger.Domain.Models;
using Ledger.Domain;

namespace Ledger.Application.Features.GetStatement;

public class GetStatementHandler : IRequestHandler<GetStatementQuery, StatementDto?>
{
    private readonly IReadModel _readModel;
    public GetStatementHandler(IReadModel readModel) => _readModel = readModel;
    public Task<StatementDto?> Handle(GetStatementQuery request, CancellationToken ct)
        => _readModel.GetStatementAsync(request.AccountId, request.PeriodStart, request.PeriodEnd, ct);
}
