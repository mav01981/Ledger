using MediatR;
using Ledger.Domain.Models;

namespace Ledger.Application.Features.GetAllAccounts;

public record GetAllAccountsQuery() : IRequest<IReadOnlyList<AccountBalanceDto>>;
