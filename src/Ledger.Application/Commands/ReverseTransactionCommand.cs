using MediatR;

namespace Ledger.Application.Commands;

public record ReverseTransactionCommand(
    Guid TransactionId,
    string IdempotencyKey
) : IRequest<CommandResult>;
