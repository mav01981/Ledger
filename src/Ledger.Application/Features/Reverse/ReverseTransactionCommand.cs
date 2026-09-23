using Ledger.Application.Shared;
using MediatR;

namespace Ledger.Application.Features.Reverse;

public record ReverseTransactionCommand(
    Guid TransactionId,
    string IdempotencyKey
) : IRequest<CommandResult>;
