using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Ledger.IntegrationTests.Infrastructure;

namespace Ledger.IntegrationTests;

[Collection("IntegrationTests")]
public class TransferTests : IntegrationTestBase
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public TransferTests(WebAppFactory factory) : base(factory) { }

    [Fact]
    public async Task Transfer_ShouldMoveFundsBetweenAccounts()
    {
        // Arrange
        var fromAccountId = Guid.NewGuid();
        var toAccountId = Guid.NewGuid();

        await TestUtilities.OpenAccountAsync(Client, fromAccountId, "open-from");
        await TestUtilities.OpenAccountAsync(Client, toAccountId, "open-to");
        await TestUtilities.DepositAsync(Client, fromAccountId, 500m, "dep-from");
        await TestUtilities.WaitForProjectionAsync(Client, fromAccountId, expectedBalance: 500m, timeoutMs: 5000);
        await TestUtilities.WaitForProjectionAsync(Client, toAccountId, expectedBalance: 0m, timeoutMs: 5000);

        // Act
        var response = await Client.PostAsJsonAsync("/api/accounts/transfer",
            new { fromAccountId, toAccountId, amount = 200m, idempotencyKey = "tx-1" });

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        Assert.Equal(fromAccountId.ToString(), result.GetProperty("aggregateId").GetString());

        // Verify both balances updated
        await TestUtilities.WaitForProjectionAsync(Client, fromAccountId, expectedBalance: 300m, timeoutMs: 5000);
        await TestUtilities.WaitForProjectionAsync(Client, toAccountId, expectedBalance: 200m, timeoutMs: 5000);

        var fromBalance = await TestUtilities.GetBalanceAsync(Client, fromAccountId);
        var toBalance = await TestUtilities.GetBalanceAsync(Client, toAccountId);
        Assert.Equal(300m, fromBalance);
        Assert.Equal(200m, toBalance);
    }

    [Fact]
    public async Task Transfer_WithInsufficientFunds_ShouldReturnBadRequest()
    {
        // Arrange
        var fromAccountId = Guid.NewGuid();
        var toAccountId = Guid.NewGuid();

        await TestUtilities.OpenAccountAsync(Client, fromAccountId, "open-from-ins");
        await TestUtilities.OpenAccountAsync(Client, toAccountId, "open-to-ins");
        await TestUtilities.WaitForProjectionAsync(Client, fromAccountId, timeoutMs: 5000);
        await TestUtilities.WaitForProjectionAsync(Client, toAccountId, timeoutMs: 5000);

        // Act
        var response = await Client.PostAsJsonAsync("/api/accounts/transfer",
            new { fromAccountId, toAccountId, amount = 100m, idempotencyKey = "tx-fail" });

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Transfer_DuplicateIdempotencyKey_ShouldReturnBadRequest()
    {
        // Arrange
        var fromAccountId = Guid.NewGuid();
        var toAccountId = Guid.NewGuid();

        await TestUtilities.OpenAccountAsync(Client, fromAccountId, "open-from-dup");
        await TestUtilities.OpenAccountAsync(Client, toAccountId, "open-to-dup");
        await TestUtilities.DepositAsync(Client, fromAccountId, 500m, "dep-from-dup");
        await TestUtilities.WaitForProjectionAsync(Client, fromAccountId, expectedBalance: 500m, timeoutMs: 5000);
        await TestUtilities.WaitForProjectionAsync(Client, toAccountId, expectedBalance: 0m, timeoutMs: 5000);

        // Act - First transfer succeeds
        var response1 = await Client.PostAsJsonAsync("/api/accounts/transfer",
            new { fromAccountId, toAccountId, amount = 100m, idempotencyKey = "tx-dup" });
        Assert.Equal(HttpStatusCode.OK, response1.StatusCode);

        // Act - Second transfer with same key fails
        var response2 = await Client.PostAsJsonAsync("/api/accounts/transfer",
            new { fromAccountId, toAccountId, amount = 100m, idempotencyKey = "tx-dup" });

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response2.StatusCode);

        // Verify balances unchanged after duplicate
        await TestUtilities.WaitForProjectionAsync(Client, fromAccountId, expectedBalance: 400m, timeoutMs: 5000);
        await TestUtilities.WaitForProjectionAsync(Client, toAccountId, expectedBalance: 100m, timeoutMs: 5000);
    }
}
