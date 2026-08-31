namespace Ledger.Infrastructure.Data;

public class AccountBalanceReadModel
{
    public Guid AccountId { get; set; }
    public string AccountType { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime OpenedAt { get; set; }
    public long LastEventVersion { get; set; }
}
