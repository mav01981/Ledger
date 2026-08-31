using MediatR;

namespace Ledger.Application.Commands;

public record TransferCommand(
    Guid FromAccountId,
    Guid ToAccountId,
    decimal Amount,
    string IdempotencyKey
) : IRequest<CommandResult>;
