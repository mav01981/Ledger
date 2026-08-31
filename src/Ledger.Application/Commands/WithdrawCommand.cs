using MediatR;

namespace Ledger.Application.Commands;

public record WithdrawCommand(
    Guid AccountId,
    decimal Amount,
    string IdempotencyKey
) : IRequest<CommandResult>;
