using MediatR;
using Ledger.Domain.Models;

namespace Ledger.Application.Queries;

public record GetAccountBalanceQuery(Guid AccountId) : IRequest<AccountBalanceDto?>;
public record GetAllAccountsQuery() : IRequest<IReadOnlyList<AccountBalanceDto>>;
public record GetTransactionHistoryQuery(Guid AccountId) : IRequest<TransactionHistoryDto?>;
public record GetStatementQuery(Guid AccountId, DateTime? PeriodStart = null, DateTime? PeriodEnd = null) : IRequest<StatementDto?>;
public record GetPointInTimeBalanceQuery(Guid AccountId, DateTime AsOf) : IRequest<decimal>;
