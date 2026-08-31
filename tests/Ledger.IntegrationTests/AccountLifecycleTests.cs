using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Ledger.IntegrationTests.Infrastructure;

namespace Ledger.IntegrationTests;

[Collection("IntegrationTests")]
public class AccountLifecycleTests : IntegrationTestBase
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public AccountLifecycleTests(WebAppFactory factory) : base(factory) { }

    [Fact]
    public async Task OpenAccount_ShouldReturnSuccess_AndCreateReadModel()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var request = new { accountId, accountType = 0, idempotencyKey = "open-1" };

        // Act
        var response = await Client.PostAsJsonAsync("/api/accounts", request);

        // Assert - Command response
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        Assert.Equal(accountId.ToString(), result.GetProperty("aggregateId").GetString());
        Assert.True(result.GetProperty("newVersion").GetInt64() >= 0);

        // Assert - Read model eventually available
        await TestUtilities.WaitForProjectionAsync(Client, accountId, timeoutMs: 5000);

        var balanceResponse = await Client.GetAsync($"/api/accounts/{accountId}");
        Assert.Equal(HttpStatusCode.OK, balanceResponse.StatusCode);
        var balance = await balanceResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        Assert.Equal("Standard", balance.GetProperty("accountType").GetString());
        Assert.Equal(0m, balance.GetProperty("balance").GetDecimal());
        Assert.Equal("Open", balance.GetProperty("status").GetString());
    }

    [Fact]
    public async Task Deposit_ShouldIncreaseBalance()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        await TestUtilities.OpenAccountAsync(Client, accountId, "open-deposit");
        await TestUtilities.WaitForProjectionAsync(Client, accountId, timeoutMs: 5000);

        // Act
        var response = await Client.PostAsJsonAsync($"/api/accounts/{accountId}/deposit",
            new { amount = 100.00m, idempotencyKey = "dep-1" });

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        Assert.Equal(accountId.ToString(), result.GetProperty("aggregateId").GetString());

        // Verify read model updated
        await TestUtilities.WaitForProjectionAsync(Client, accountId, expectedBalance: 100m, timeoutMs: 5000);

        var balanceResponse = await Client.GetAsync($"/api/accounts/{accountId}");
        var balance = await balanceResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        Assert.Equal(100m, balance.GetProperty("balance").GetDecimal());
    }

    [Fact]
    public async Task Withdraw_ShouldDecreaseBalance()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        await TestUtilities.OpenAccountAsync(Client, accountId, "open-withdraw");
        await TestUtilities.DepositAsync(Client, accountId, 200m, "dep-withdraw");
        await TestUtilities.WaitForProjectionAsync(Client, accountId, expectedBalance: 200m, timeoutMs: 5000);

        // Act
        var response = await Client.PostAsJsonAsync($"/api/accounts/{accountId}/withdraw",
            new { amount = 75.50m, idempotencyKey = "wd-1" });

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        await TestUtilities.WaitForProjectionAsync(Client, accountId, expectedBalance: 124.50m, timeoutMs: 5000);

        var balanceResponse = await Client.GetAsync($"/api/accounts/{accountId}");
        var balance = await balanceResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        Assert.Equal(124.50m, balance.GetProperty("balance").GetDecimal());
    }
}
