using Ledger.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Ledger.IntegrationTests.Infrastructure;

/// <summary>
/// Custom WebApplicationFactory that configures the API to use
/// a Testcontainers PostgreSQL instance instead of a local database.
/// Implements IAsyncLifetime for proper async initialization in xUnit.
/// </summary>
public class WebAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgresFixture _postgresFixture;
    private bool _initialized;

    public WebAppFactory()
    {
        _postgresFixture = new PostgresFixture();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove the existing DbContext registration
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<LedgerDbContext>));
            if (descriptor != null)
                services.Remove(descriptor);

            // Add DbContext pointing to Testcontainers PostgreSQL
            services.AddDbContext<LedgerDbContext>(options =>
                options.UseNpgsql(_postgresFixture.ConnectionString));
        });

        builder.UseEnvironment("Development");
    }

    public async Task InitializeAsync()
    {
        if (!_initialized)
        {
            await _postgresFixture.InitializeAsync();

            // Ensure database schema is created
            using var scope = Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<LedgerDbContext>();
            await context.Database.EnsureCreatedAsync();
            _initialized = true;
        }
    }

    public new async Task DisposeAsync()
    {
        await _postgresFixture.DisposeAsync();
        await base.DisposeAsync();
    }

    /// <summary>
    /// Creates a new HttpClient and ensures the database is initialized.
    /// Call this in the test constructor.
    /// </summary>
    public async Task<HttpClient> CreateClientAsync()
    {
        await InitializeAsync();
        return CreateClient();
    }

    /// <summary>
    /// Resets the database state between tests for test isolation.
    /// </summary>
    public async Task ResetDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<LedgerDbContext>();

        // Clear all data from tables
        context.Database.ExecuteSqlRaw(@"
            TRUNCATE TABLE events, snapshots, transfers, idempotency_records,
                         outbox, account_balances, transaction_history RESTART IDENTITY CASCADE");
    }
}
