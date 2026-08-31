using Ledger.Domain;

namespace Ledger.Infrastructure.Data;

public class TransferRecord
{
    public Guid TransactionId { get; set; }
    public string Lines { get; set; } = string.Empty;
    public TransferStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
}
