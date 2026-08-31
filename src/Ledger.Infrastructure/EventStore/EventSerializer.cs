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

    private static readonly Dictionary<Type, string> TypeMap = new()
    {
        { typeof(AccountOpened), nameof(AccountOpened) },
        { typeof(FundsDeposited), nameof(FundsDeposited) },
        { typeof(FundsWithdrawn), nameof(FundsWithdrawn) },
        { typeof(TransferPosted), nameof(TransferPosted) },
        { typeof(TransactionReversed), nameof(TransactionReversed) }
    };

    private static readonly Dictionary<string, string> ReverseTypeMap = new()
    {
        { nameof(AccountOpened), "Ledger.Domain.Events.AccountOpened" },
        { nameof(FundsDeposited), "Ledger.Domain.Events.FundsDeposited" },
        { typeof(FundsWithdrawn).Name, "Ledger.Domain.Events.FundsWithdrawn" },
        { nameof(TransferPosted), "Ledger.Domain.Events.TransferPosted" },
        { nameof(TransactionReversed), "Ledger.Domain.Events.TransactionReversed" }
    };

    public static string Serialize(DomainEvent @event)
    {
        var json = JsonSerializer.Serialize(@event, @event.GetType(), Options);
        return json;
    }

    public static DomainEvent Deserialize(string eventType, string payload)
    {
        return eventType switch
        {
            nameof(AccountOpened) => JsonSerializer.Deserialize<AccountOpened>(payload, Options)!,
            nameof(FundsDeposited) => JsonSerializer.Deserialize<FundsDeposited>(payload, Options)!,
            nameof(FundsWithdrawn) => JsonSerializer.Deserialize<FundsWithdrawn>(payload, Options)!,
            nameof(TransferPosted) => JsonSerializer.Deserialize<TransferPosted>(payload, Options)!,
            nameof(TransactionReversed) => JsonSerializer.Deserialize<TransactionReversed>(payload, Options)!,
            _ => throw new InvalidOperationException($"Unknown event type: {eventType}")
        };
    }

    public static string GetEventTypeName(DomainEvent @event)
    {
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
}
