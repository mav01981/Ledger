using MediatR;

namespace Ledger.Application.Features.GetPointInTimeBalance;

public record GetPointInTimeBalanceQuery(Guid AccountId, DateTime AsOf) : IRequest<decimal>;
