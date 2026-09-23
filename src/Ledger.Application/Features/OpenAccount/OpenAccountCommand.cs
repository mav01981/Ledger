using Ledger.Application.Shared;
using MediatR;
using Ledger.Domain;

namespace Ledger.Application.Features.OpenAccount;

public record OpenAccountCommand(
    Guid AccountId,
    AccountType AccountType,
    string IdempotencyKey
) : IRequest<CommandResult>;
