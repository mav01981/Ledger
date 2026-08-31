using Ledger.Domain.Events;

namespace Ledger.Domain;

public interface ITransactionStore
{
    Task<Transfer?> GetByIdAsync(Guid transactionId, CancellationToken ct = default);
    Task SaveAsync(Transfer transfer, CancellationToken ct = default);
}

public class Transfer
{
    public Guid TransactionId { get; set; }
    public List<TransactionLine> Lines { get; set; } = new();
    public TransferStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
}

public enum TransferStatus
{
    Posted = 0,
    Reversed = 1
}
