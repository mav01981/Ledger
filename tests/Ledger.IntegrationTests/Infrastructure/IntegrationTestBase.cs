namespace Ledger.IntegrationTests.Infrastructure;

/// <summary>
/// Base class for integration tests that provides a shared HttpClient
/// and automatic database reset between tests.
/// </summary>
public abstract class IntegrationTestBase : IAsyncLifetime
{
    protected readonly WebAppFactory Factory;
    protected HttpClient Client { get; private set; } = null!;

    protected IntegrationTestBase(WebAppFactory factory)
    {
        Factory = factory;
    }

    public async Task InitializeAsync()
    {
        Client = await Factory.CreateClientAsync();
        await Factory.ResetDatabaseAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;
}
