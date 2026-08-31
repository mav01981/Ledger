using MediatR;

namespace Ledger.Application.Commands;

public record DepositCommand(
    Guid AccountId,
    decimal Amount,
    string IdempotencyKey
) : IRequest<CommandResult>;
