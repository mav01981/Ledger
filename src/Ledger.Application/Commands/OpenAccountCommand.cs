using MediatR;
using Ledger.Domain;

namespace Ledger.Application.Commands;

public record OpenAccountCommand(
    Guid AccountId,
    AccountType AccountType,
    string IdempotencyKey
) : IRequest<CommandResult>;

public record CommandResult(
    bool Success,
    Guid AggregateId,
    long NewVersion,
    string? Error = null)
{
    public static CommandResult Ok(Guid id, long version) => new(true, id, version);
    public static CommandResult Fail(string error) => new(false, Guid.Empty, -1, error);
}
