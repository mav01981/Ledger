using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Ledger.IntegrationTests.Infrastructure;

namespace Ledger.IntegrationTests;

[Collection("IntegrationTests")]
public class ReadModelTests : IntegrationTestBase
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public ReadModelTests(WebAppFactory factory) : base(factory) { }

    [Fact]
    public async Task GetAllAccounts_ShouldReturnAllAccounts()
    {
        // Arrange
        var account1 = Guid.NewGuid();
        var account2 = Guid.NewGuid();

        await TestUtilities.OpenAccountAsync(Client, account1, "open-all-1");
        await TestUtilities.OpenAccountAsync(Client, account2, "open-all-2");
        await TestUtilities.WaitForProjectionAsync(Client, account1, timeoutMs: 5000);
        await TestUtilities.WaitForProjectionAsync(Client, account2, timeoutMs: 5000);

        // Act
        var response = await Client.GetAsync("/api/accounts");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var accounts = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var items = accounts.EnumerateArray().ToList();
        Assert.True(items.Count >= 2);
    }

    [Fact]
    public async Task GetTransactionHistory_ShouldReturnAllTransactions()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        await TestUtilities.OpenAccountAsync(Client, accountId, "open-hist");
        await TestUtilities.DepositAsync(Client, accountId, 100m, "dep-hist-1");
        await TestUtilities.DepositAsync(Client, accountId, 50m, "dep-hist-2");
        await TestUtilities.WaitForProjectionAsync(Client, accountId, expectedBalance: 150m, timeoutMs: 5000);

        // Act
        var response = await Client.GetAsync($"/api/accounts/{accountId}/history");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var history = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var transactions = history.GetProperty("transactions").EnumerateArray().ToList();
        Assert.Equal(3, transactions.Count); // Open + 2 deposits
    }

    [Fact]
    public async Task GetStatement_ShouldReturnFilteredTransactions()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        await TestUtilities.OpenAccountAsync(Client, accountId, "open-stmt");
        await TestUtilities.DepositAsync(Client, accountId, 200m, "dep-stmt");
        await TestUtilities.WaitForProjectionAsync(Client, accountId, expectedBalance: 200m, timeoutMs: 5000);

        // Act
        var from = DateTime.UtcNow.AddHours(-1).ToString("o");
        var to = DateTime.UtcNow.AddHours(1).ToString("o");
        var response = await Client.GetAsync($"/api/accounts/{accountId}/statement?from={from}&to={to}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var statement = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        Assert.Equal(accountId.ToString(), statement.GetProperty("accountId").GetString());
        Assert.Equal(200m, statement.GetProperty("closingBalance").GetDecimal());
    }

    [Fact]
    public async Task GetBalanceAt_ShouldReturnPointInTimeBalance()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        await TestUtilities.OpenAccountAsync(Client, accountId, "open-pit");
        await TestUtilities.DepositAsync(Client, accountId, 100m, "dep-pit-1");

        // Wait for projection
        await TestUtilities.WaitForProjectionAsync(Client, accountId, expectedBalance: 100m, timeoutMs: 5000);

        // Record the time
        var asOf = DateTime.UtcNow;
        await Task.Delay(100); // Small delay to ensure timestamp difference

        // Deposit more after the timestamp
        await TestUtilities.DepositAsync(Client, accountId, 50m, "dep-pit-2");
        await TestUtilities.WaitForProjectionAsync(Client, accountId, expectedBalance: 150m, timeoutMs: 5000);

        // Act
        var response = await Client.GetAsync($"/api/accounts/{accountId}/balance-at?asOf={asOf:o}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        Assert.Equal(100m, result.GetProperty("balance").GetDecimal());
    }
}
