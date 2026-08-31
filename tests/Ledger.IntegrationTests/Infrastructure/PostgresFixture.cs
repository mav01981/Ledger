using Testcontainers.PostgreSql;

namespace Ledger.IntegrationTests.Infrastructure;

/// <summary>
/// Shared PostgreSQL container fixture for integration tests.
/// The container is created once per test collection and shared across tests.
/// </summary>
public class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container;

#pragma warning disable CS0618 // PostgreSqlBuilder parameterless constructor is obsolete but still required
    public PostgresFixture()
    {
        _container = new PostgreSqlBuilder()
            .WithImage("postgres:16")
            .WithDatabase("ledger_test")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .WithPortBinding(5432, true)
            .Build();
    }
#pragma warning restore CS0618

    public string ConnectionString => _container.GetConnectionString();

    public Task InitializeAsync() => _container.StartAsync();

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}
