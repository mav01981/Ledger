namespace Ledger.Domain;

public interface ISnapshotStore
{
    Task<AccountSnapshot?> GetLatestAsync(Guid accountId, CancellationToken ct = default);
    Task SaveAsync(AccountSnapshot snapshot, CancellationToken ct = default);
}

public sealed record AccountSnapshot(
    Guid AccountId,
    long Version,
    AccountType AccountType,
    AccountStatus AccountStatus,
    Money Balance,
    DateTime SnapshottedAt
);
