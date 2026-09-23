using Ledger.Application.Shared;
using MediatR;

namespace Ledger.Application.Features.Transfer;

public record TransferCommand(
    Guid FromAccountId,
    Guid ToAccountId,
    decimal Amount,
    string IdempotencyKey
) : IRequest<CommandResult>;
