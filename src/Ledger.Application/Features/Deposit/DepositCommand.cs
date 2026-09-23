using Ledger.Application.Features.OpenAccount;
using Ledger.Application.Shared;
using MediatR;

namespace Ledger.Application.Features.Deposit;

public record DepositCommand(
    Guid AccountId,
    decimal Amount,
    string IdempotencyKey
) : IRequest<CommandResult>;
