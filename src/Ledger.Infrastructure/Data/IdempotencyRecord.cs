namespace Ledger.Infrastructure.Data;

public class IdempotencyRecord
{
    public string Key { get; set; } = string.Empty;
    public Guid AggregateId { get; set; }
    public DateTime ProcessedAt { get; set; }
}
