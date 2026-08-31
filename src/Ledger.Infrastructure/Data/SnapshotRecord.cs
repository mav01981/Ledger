namespace Ledger.Infrastructure.Data;

public class SnapshotRecord
{
    public Guid AggregateId { get; set; }
    public long Version { get; set; }
    public string State { get; set; } = string.Empty;
    public DateTime SnapshottedAt { get; set; }
}
