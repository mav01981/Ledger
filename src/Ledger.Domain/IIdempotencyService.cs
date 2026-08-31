namespace Ledger.Domain;

public interface IIdempotencyService
{
    Task<bool> HasBeenProcessedAsync(string idempotencyKey, CancellationToken ct = default);
    Task MarkProcessedAsync(string idempotencyKey, Guid aggregateId, CancellationToken ct = default);
}
