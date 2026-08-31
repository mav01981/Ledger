using Ledger.Domain.Models;

namespace Ledger.Domain;

public interface IReadModel
{
    Task<AccountBalanceDto?> GetAccountBalanceAsync(Guid accountId, CancellationToken ct = default);
    Task<IReadOnlyList<AccountBalanceDto>> GetAllAccountsAsync(CancellationToken ct = default);
    Task<TransactionHistoryDto?> GetTransactionHistoryAsync(Guid accountId, CancellationToken ct = default);
    Task<StatementDto?> GetStatementAsync(Guid accountId, DateTime? periodStart, DateTime? periodEnd, CancellationToken ct = default);
}
