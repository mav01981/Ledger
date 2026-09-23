using MediatR;
using Ledger.Domain.Models;

namespace Ledger.Application.Features.GetTransactionHistory;

public record GetTransactionHistoryQuery(Guid AccountId) : IRequest<TransactionHistoryDto?>;
