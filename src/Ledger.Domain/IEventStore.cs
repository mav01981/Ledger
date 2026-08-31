using Ledger.Domain.Events;

namespace Ledger.Domain;

public interface IEventStore
{
    Task<IReadOnlyList<DomainEvent>> ReadStreamAsync(Guid streamId, long fromVersion = -1, CancellationToken ct = default);
    Task AppendToStreamAsync(Guid streamId, IReadOnlyList<DomainEvent> events, long expectedVersion, CancellationToken ct = default);
    Task<IReadOnlyList<DomainEvent>> ReadAllAsync(long afterPosition = -1, CancellationToken ct = default);
}
