using Ledger.Domain.Models;
using MediatR;

namespace Ledger.Application.Features.GetAccountBalance;

public record GetAccountBalanceQuery(Guid AccountId) : IRequest<AccountBalanceDto?>;
