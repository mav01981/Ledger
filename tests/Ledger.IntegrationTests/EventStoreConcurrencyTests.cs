using Ledger.Domain;
using Ledger.Domain.Events;
using Ledger.IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Ledger.IntegrationTests;

[Collection("IntegrationTests")]
public class EventStoreConcurrencyTests : IntegrationTestBase
{
    public EventStoreConcurrencyTests(WebAppFactory factory) : base(factory) { }

    [Fact]
    public async Task AppendToStream_WhenExpectedVersionIsMinusOneAndStreamExists_Throws()
    {
        var streamId = Guid.NewGuid();
        var opened = AccountOpened.Create(
            streamId,
            AccountType.Standard,
            DateTime.UtcNow);

        using var scope = Factory.Services.CreateScope();
        var eventStore = scope.ServiceProvider.GetRequiredService<IEventStore>();

        await eventStore.AppendToStreamAsync(streamId, [opened], expectedVersion: -1);

        var duplicateOpen = AccountOpened.Create(
            streamId,
            AccountType.Standard,
            DateTime.UtcNow);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            eventStore.AppendToStreamAsync(streamId, [duplicateOpen], expectedVersion: -1));

        Assert.Single(await eventStore.ReadStreamAsync(streamId));
    }
}
