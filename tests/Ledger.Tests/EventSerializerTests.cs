using System.Text.Json;
using Ledger.Domain;
using Ledger.Domain.Events;
using Ledger.Infrastructure.EventStore;

namespace Ledger.Tests;

public class EventSerializerTests
{
    [Fact]
    public void Deserialize_NullPayload_ThrowsJsonException()
    {
        Assert.Throws<JsonException>(() =>
            EventSerializer.Deserialize(nameof(AccountOpened), "null"));
    }

    [Fact]
    public void Deserialize_UnknownEventType_ThrowsInvalidOperationException()
    {
        Assert.Throws<InvalidOperationException>(() =>
            EventSerializer.Deserialize("UnknownEvent", "{}"));
    }

    [Fact]
    public void SerializeAndDeserialize_PreservesAccountOpenedEvent()
    {
        var expected = AccountOpened.Create(
            Guid.NewGuid(),
            AccountType.Standard,
            new DateTime(2026, 10, 10, 12, 0, 0, DateTimeKind.Utc));

        var payload = EventSerializer.Serialize(expected);
        var actual = EventSerializer.Deserialize(nameof(AccountOpened), payload);

        Assert.Equal(expected, actual);
    }
}
