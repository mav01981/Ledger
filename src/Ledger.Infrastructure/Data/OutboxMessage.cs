namespace Ledger.Infrastructure.Data;

public class OutboxMessage
{
    public Guid Id { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public DateTime? ProcessedAt { get; set; }
}
