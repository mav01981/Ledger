namespace Ledger.IntegrationTests.Infrastructure;

/// <summary>
/// Collection definition to ensure tests using WebAppFactory
/// don't run in parallel against the same database.
/// </summary>
[CollectionDefinition("IntegrationTests")]
public class CollectionDefinition : ICollectionFixture<WebAppFactory>
{
    // This class has no code; it's just a marker for the collection fixture.
}
