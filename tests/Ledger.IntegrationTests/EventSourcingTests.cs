using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Ledger.IntegrationTests.Infrastructure;

namespace Ledger.IntegrationTests;

/// <summary>
/// Tests that verify event sourcing capabilities:
/// - Point-in-time balance reconstruction
/// - Event replay for state reconstruction
/// </summary>
[Collection("IntegrationTests")]
public class EventSourcingTests : IntegrationTestBase
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public EventSourcingTests(WebAppFactory factory) : base(factory) { }

    [Fact]
    public async Task BalanceAt_ShouldReturnHistoricalBalance()
    {
        // Arrange - Create account with multiple transactions over time
        var accountId = Guid.NewGuid();
        await TestUtilities.OpenAccountAsync(Client, accountId, "es-open");

        // Deposit 500
        await TestUtilities.DepositAsync(Client, accountId, 500m, "es-dep1");
        await TestUtilities.WaitForProjectionAsync(Client, accountId, expectedBalance: 500m, timeoutMs: 5000);

        // Record time after first deposit
        var afterFirstDeposit = DateTime.UtcNow;
        await Task.Delay(200);

        // Deposit 300 more
        await TestUtilities.DepositAsync(Client, accountId, 300m, "es-dep2");
        await TestUtilities.WaitForProjectionAsync(Client, accountId, expectedBalance: 800m, timeoutMs: 5000);

        // Record time after second deposit
        var afterSecondDeposit = DateTime.UtcNow;
        await Task.Delay(200);

        // Withdraw 200
        await TestUtilities.WithdrawAsync(Client, accountId, 200m, "es-wd");
        await TestUtilities.WaitForProjectionAsync(Client, accountId, expectedBalance: 600m, timeoutMs: 5000);

        // Act & Assert - Check point-in-time balances
        var balanceAfterFirst = await TestUtilities.GetBalanceAtAsync(Client, accountId, afterFirstDeposit);
        Assert.Equal(500m, balanceAfterFirst);

        var balanceAfterSecond = await TestUtilities.GetBalanceAtAsync(Client, accountId, afterSecondDeposit);
        Assert.Equal(800m, balanceAfterSecond);

        var currentBalance = await TestUtilities.GetBalanceAsync(Client, accountId);
        Assert.Equal(600m, currentBalance);
    }

    [Fact]
    public async Task BalanceAt_BeforeAnyTransaction_ShouldReturnZero()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        await TestUtilities.OpenAccountAsync(Client, accountId, "es-open-zero");
        await TestUtilities.DepositAsync(Client, accountId, 100m, "es-dep-zero");
        await TestUtilities.WaitForProjectionAsync(Client, accountId, expectedBalance: 100m, timeoutMs: 5000);

        // Act - Query balance before account existed
        var beforeAccount = DateTime.UtcNow.AddHours(-1);
        var balance = await TestUtilities.GetBalanceAtAsync(Client, accountId, beforeAccount);

        // Assert
        Assert.Equal(0m, balance);
    }
}
