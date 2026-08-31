using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Ledger.IntegrationTests.Infrastructure;

namespace Ledger.IntegrationTests;

[Collection("IntegrationTests")]
public class ValidationTests : IntegrationTestBase
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public ValidationTests(WebAppFactory factory) : base(factory) { }

    [Fact]
    public async Task Withdraw_WithInsufficientFunds_ShouldReturnBadRequest()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        await TestUtilities.OpenAccountAsync(Client, accountId, "open-overdraft");
        await TestUtilities.WaitForProjectionAsync(Client, accountId, timeoutMs: 5000);

        // Act
        var response = await Client.PostAsJsonAsync($"/api/accounts/{accountId}/withdraw",
            new { amount = 50.00m, idempotencyKey = "wd-fail" });

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        Assert.NotNull(error.GetProperty("error").GetString());
    }

    [Fact]
    public async Task OpenAccount_DuplicateIdempotencyKey_ShouldReturnConflict()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        await TestUtilities.OpenAccountAsync(Client, accountId, "dup-key");

        // Act - Try to open with same idempotency key
        var response = await Client.PostAsJsonAsync("/api/accounts",
            new { accountId = Guid.NewGuid(), accountType = 0, idempotencyKey = "dup-key" });

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Deposit_DuplicateIdempotencyKey_ShouldReturnBadRequest()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        await TestUtilities.OpenAccountAsync(Client, accountId, "open-dup-dep");
        await TestUtilities.DepositAsync(Client, accountId, 100m, "dup-dep-key");
        await TestUtilities.WaitForProjectionAsync(Client, accountId, expectedBalance: 100m, timeoutMs: 5000);

        // Act - Try deposit with same idempotency key
        var response = await Client.PostAsJsonAsync($"/api/accounts/{accountId}/deposit",
            new { amount = 200m, idempotencyKey = "dup-dep-key" });

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        // Verify balance unchanged
        var balanceResponse = await Client.GetAsync($"/api/accounts/{accountId}");
        var balance = await balanceResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        Assert.Equal(100m, balance.GetProperty("balance").GetDecimal());
    }

    [Fact]
    public async Task Deposit_ToNonExistentAccount_ShouldReturnBadRequest()
    {
        // Act
        var response = await Client.PostAsJsonAsync($"/api/accounts/{Guid.NewGuid()}/deposit",
            new { amount = 100m, idempotencyKey = "dep-noexist" });

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetBalance_NonExistentAccount_ShouldReturnNotFound()
    {
        // Act
        var response = await Client.GetAsync($"/api/accounts/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
