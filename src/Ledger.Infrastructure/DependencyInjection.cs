using Ledger.Domain;
using Ledger.Infrastructure.Data;
using Ledger.Infrastructure.EventStore;
using Ledger.Infrastructure.Idempotency;
using Ledger.Infrastructure.Outbox;
using Ledger.Infrastructure.ReadModel;
using Ledger.Infrastructure.Snapshots;
using Ledger.Infrastructure.Transactions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Ledger.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddLedgerInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("LedgerDb")
            ?? "Host=localhost;Database=ledger;Username=postgres;Password=postgres";

        services.AddDbContext<LedgerDbContext>(options =>
            options.UseNpgsql(connectionString));

        // Time is a dependency like any other: TryAdd keeps a test/fake registration if one is already present.
        services.TryAddSingleton(TimeProvider.System);

        services.AddScoped<IEventStore, PostgresEventStore>();
        services.AddScoped<ISnapshotStore, PostgresSnapshotStore>();
        services.AddScoped<ITransactionStore, PostgresTransactionStore>();
        services.AddScoped<IIdempotencyService, DbIdempotencyService>();
        services.AddScoped<IReadModel, PostgresReadModel>();

        services.AddScoped<OutboxWriter>();
        services.AddScoped<ProjectionUpdater>();

        services.AddHostedService<OutboxRelay>();

        return services;
    }
}
