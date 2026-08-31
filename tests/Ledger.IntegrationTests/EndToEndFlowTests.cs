using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Ledger.IntegrationTests.Infrastructure;

namespace Ledger.IntegrationTests;

/// <summary>
/// End-to-end tests that demonstrate the full CQRS + Event Sourcing workflow:
/// Commands → Event Store → Outbox → Projections → Read Models
/// </summary>
[Collection("IntegrationTests")]
public class EndToEndFlowTests : IntegrationTestBase
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public EndToEndFlowTests(WebAppFactory factory) : base(factory) { }

    [Fact]
    public async Task FullLifecycle_ShouldMaintainConsistentState()
    {
        // === WRITE SIDE: Execute commands ===

        // 1. Open two accounts
        var wallet = Guid.NewGuid();
        var savings = Guid.NewGuid();

        await TestUtilities.OpenAccountAsync(Client, wallet, "e2e-open-wallet");
        await TestUtilities.OpenAccountAsync(Client, savings, "e2e-open-savings");

        // 2. Deposit into wallet
        await TestUtilities.DepositAsync(Client, wallet, 1000m, "e2e-dep-1");
        await TestUtilities.WaitForProjectionAsync(Client, wallet, expectedBalance: 1000m, timeoutMs: 5000);
        await TestUtilities.WaitForProjectionAsync(Client, savings, expectedBalance: 0m, timeoutMs: 5000);

        // 3. Transfer from wallet to savings
        await TestUtilities.TransferAsync(Client, wallet, savings, 300m, "e2e-tx-1");
        await TestUtilities.WaitForProjectionAsync(Client, wallet, expectedBalance: 700m, timeoutMs: 5000);
        await TestUtilities.WaitForProjectionAsync(Client, savings, expectedBalance: 300m, timeoutMs: 5000);

        // 4. Withdraw from wallet
        await TestUtilities.WithdrawAsync(Client, wallet, 200m, "e2e-wd-1");
        await TestUtilities.WaitForProjectionAsync(Client, wallet, expectedBalance: 500m, timeoutMs: 5000);

        // === READ SIDE: Verify projections ===

        // 5. Verify final balances
        var walletBalance = await TestUtilities.GetBalanceAsync(Client, wallet);
        var savingsBalance = await TestUtilities.GetBalanceAsync(Client, savings);
        Assert.Equal(500m, walletBalance);
        Assert.Equal(300m, savingsBalance);

        // 6. Verify transaction history
        var walletHistory = await TestUtilities.GetHistoryAsync(Client, wallet);
        var walletTxns = walletHistory.GetProperty("transactions").EnumerateArray().ToList();
        Assert.Equal(4, walletTxns.Count); // Open + Deposit + Transfer Out + Withdraw

        var savingsHistory = await TestUtilities.GetHistoryAsync(Client, savings);
        var savingsTxns = savingsHistory.GetProperty("transactions").EnumerateArray().ToList();
        Assert.Equal(2, savingsTxns.Count); // Open + Transfer In

        // 7. Verify all accounts listed
        var allAccountsResponse = await Client.GetAsync("/api/accounts");
        var allAccounts = await allAccountsResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var accountsList = allAccounts.EnumerateArray().ToList();
        Assert.True(accountsList.Count >= 2);
    }
}
