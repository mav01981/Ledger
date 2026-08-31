namespace Ledger.Infrastructure.Data;

public class TransactionHistoryReadModel
{
    public Guid AccountId { get; set; }
    public Guid TransactionId { get; set; }
    public decimal Amount { get; set; }
    public string Direction { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public decimal RunningBalance { get; set; }
}
