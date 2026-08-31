using System.Text.Json;
using System.Net.Http.Json;

namespace Ledger.IntegrationTests.Infrastructure;

/// <summary>
/// Shared helper methods for integration tests.
/// </summary>
public static class TestUtilities
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static async Task OpenAccountAsync(HttpClient client, Guid accountId, string idempotencyKey)
    {
        var response = await client.PostAsJsonAsync("/api/accounts",
            new { accountId, accountType = 0, idempotencyKey });
        response.EnsureSuccessStatusCode();
    }

    public static async Task DepositAsync(HttpClient client, Guid accountId, decimal amount, string idempotencyKey)
    {
        var response = await client.PostAsJsonAsync($"/api/accounts/{accountId}/deposit",
            new { amount, idempotencyKey });
        response.EnsureSuccessStatusCode();
    }

    public static async Task WithdrawAsync(HttpClient client, Guid accountId, decimal amount, string idempotencyKey)
    {
        var response = await client.PostAsJsonAsync($"/api/accounts/{accountId}/withdraw",
            new { amount, idempotencyKey });
        response.EnsureSuccessStatusCode();
    }

    public static async Task TransferAsync(HttpClient client, Guid fromId, Guid toId, decimal amount, string idempotencyKey)
    {
        var response = await client.PostAsJsonAsync("/api/accounts/transfer",
            new { fromAccountId = fromId, toAccountId = toId, amount, idempotencyKey });
        response.EnsureSuccessStatusCode();
    }

    public static async Task<decimal> GetBalanceAsync(HttpClient client, Guid accountId)
    {
        var response = await client.GetAsync($"/api/accounts/{accountId}");
        response.EnsureSuccessStatusCode();
        var balance = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        return balance.GetProperty("balance").GetDecimal();
    }

    public static async Task<JsonElement> GetHistoryAsync(HttpClient client, Guid accountId)
    {
        var response = await client.GetAsync($"/api/accounts/{accountId}/history");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
    }

    public static async Task<decimal> GetBalanceAtAsync(HttpClient client, Guid accountId, DateTime asOf)
    {
        var response = await client.GetAsync($"/api/accounts/{accountId}/balance-at?asOf={asOf:o}");
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        return result.GetProperty("balance").GetDecimal();
    }

    /// <summary>
    /// Polls the account balance endpoint until it matches the expected balance or times out.
    /// This handles the eventual consistency of the projection.
    /// </summary>
    public static async Task WaitForProjectionAsync(
        HttpClient client,
        Guid accountId,
        decimal? expectedBalance = null,
        int timeoutMs = 5000)
    {
        var startTime = DateTime.UtcNow;
        while ((DateTime.UtcNow - startTime).TotalMilliseconds < timeoutMs)
        {
            var response = await client.GetAsync($"/api/accounts/{accountId}");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
                if (expectedBalance == null)
                    return;
                if (content.GetProperty("balance").GetDecimal() == expectedBalance)
                    return;
            }
            await Task.Delay(100);
        }
        throw new TimeoutException($"Projection not updated within {timeoutMs}ms");
    }
}
