using Ledger.Application.Shared;
using MediatR;

namespace Ledger.Application.Features.Withdraw;

public record WithdrawCommand(
    Guid AccountId,
    decimal Amount,
    string IdempotencyKey
) : IRequest<CommandResult>;
