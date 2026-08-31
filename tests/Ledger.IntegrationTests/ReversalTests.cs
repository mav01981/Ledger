using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Ledger.IntegrationTests.Infrastructure;

namespace Ledger.IntegrationTests;

[Collection("IntegrationTests")]
public class ReversalTests : IntegrationTestBase
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public ReversalTests(WebAppFactory factory) : base(factory) { }

    [Fact]
    public async Task ReverseTransaction_ShouldReverseTransfer()
    {
        // Arrange
        var fromAccountId = Guid.NewGuid();
        var toAccountId = Guid.NewGuid();

        await TestUtilities.OpenAccountAsync(Client, fromAccountId, "open-from-rev");
        await TestUtilities.OpenAccountAsync(Client, toAccountId, "open-to-rev");
        await TestUtilities.DepositAsync(Client, fromAccountId, 500m, "dep-from-rev");
        await TestUtilities.WaitForProjectionAsync(Client, fromAccountId, expectedBalance: 500m, timeoutMs: 5000);
        await TestUtilities.WaitForProjectionAsync(Client, toAccountId, expectedBalance: 0m, timeoutMs: 5000);

        // Perform transfer
        var transferResponse = await Client.PostAsJsonAsync("/api/accounts/transfer",
            new { fromAccountId, toAccountId, amount = 200m, idempotencyKey = "tx-rev" });
        Assert.Equal(HttpStatusCode.OK, transferResponse.StatusCode);

        await TestUtilities.WaitForProjectionAsync(Client, fromAccountId, expectedBalance: 300m, timeoutMs: 5000);
        await TestUtilities.WaitForProjectionAsync(Client, toAccountId, expectedBalance: 200m, timeoutMs: 5000);

        // Get the transfer transaction ID from history
        var transferId = await GetTransferIdAsync(fromAccountId, 200m);

        // Act - Reverse the transfer
        var reverseResponse = await Client.PostAsJsonAsync("/api/accounts/reverse",
            new { transactionId = transferId, idempotencyKey = "rev-1" });

        // Assert
        Assert.Equal(HttpStatusCode.OK, reverseResponse.StatusCode);

        // Verify balances restored
        await TestUtilities.WaitForProjectionAsync(Client, fromAccountId, expectedBalance: 500m, timeoutMs: 5000);
        await TestUtilities.WaitForProjectionAsync(Client, toAccountId, expectedBalance: 0m, timeoutMs: 5000);
    }

    [Fact]
    public async Task ReverseTransaction_NonExistent_ShouldReturnBadRequest()
    {
        // Act
        var response = await Client.PostAsJsonAsync("/api/accounts/reverse",
            new { transactionId = Guid.NewGuid(), idempotencyKey = "rev-fail" });

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private async Task<Guid> GetTransferIdAsync(Guid accountId, decimal expectedAmount)
    {
        var response = await Client.GetAsync($"/api/accounts/{accountId}/history");
        response.EnsureSuccessStatusCode();
        var history = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var transactions = history.GetProperty("transactions").EnumerateArray().ToList();
        // Transfer out is recorded as positive amount with "Debit" direction
        var transferTx = transactions.First(t => t.GetProperty("amount").GetDecimal() == expectedAmount && t.GetProperty("direction").GetString() == "Debit");
        return Guid.Parse(transferTx.GetProperty("transactionId").GetString()!);
    }
}
