using Ledger.Domain.Events;

namespace Ledger.Infrastructure.Data;

public class EventRecord
{
    public Guid StreamId { get; set; }
    public long StreamPosition { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public long GlobalPosition { get; set; }
}
