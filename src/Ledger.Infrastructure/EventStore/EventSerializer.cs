using System.Text.Json;
using System.Text.Json.Serialization;
using Ledger.Domain.Events;

namespace Ledger.Infrastructure.EventStore;

public static class EventSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        Converters = { new JsonStringEnumConverter() }
    };

    public static string Serialize(DomainEvent @event)
    {
        ArgumentNullException.ThrowIfNull(@event);
        return JsonSerializer.Serialize(@event, @event.GetType(), Options);
    }

    public static DomainEvent Deserialize(string eventType, string payload)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventType);
        ArgumentNullException.ThrowIfNull(payload);

        return eventType switch
        {
            nameof(AccountOpened) => Deserialize<AccountOpened>(payload),
            nameof(FundsDeposited) => Deserialize<FundsDeposited>(payload),
            nameof(FundsWithdrawn) => Deserialize<FundsWithdrawn>(payload),
            nameof(TransferPosted) => Deserialize<TransferPosted>(payload),
            nameof(TransactionReversed) => Deserialize<TransactionReversed>(payload),
            _ => throw new InvalidOperationException($"Unknown event type: {eventType}")
        };
    }

    public static string GetEventTypeName(DomainEvent @event)
    {
        ArgumentNullException.ThrowIfNull(@event);

        return @event switch
        {
            AccountOpened => nameof(AccountOpened),
            FundsDeposited => nameof(FundsDeposited),
            FundsWithdrawn => nameof(FundsWithdrawn),
            TransferPosted => nameof(TransferPosted),
            TransactionReversed => nameof(TransactionReversed),
            _ => throw new InvalidOperationException($"Unknown event type: {@event.GetType()}")
        };
    }

    private static T Deserialize<T>(string payload) where T : DomainEvent =>
        JsonSerializer.Deserialize<T>(payload, Options)
        ?? throw new JsonException($"The payload for {typeof(T).Name} deserialized to null.");
}
