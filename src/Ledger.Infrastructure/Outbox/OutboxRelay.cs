using System.Text.Json;
using Ledger.Domain.Events;
using Ledger.Infrastructure.Data;
using Ledger.Infrastructure.EventStore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Ledger.Infrastructure.Outbox;

public class OutboxRelay : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OutboxRelay> _logger;

    public OutboxRelay(IServiceProvider serviceProvider, ILogger<OutboxRelay> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingMessagesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing outbox messages");
            }

            await Task.Delay(TimeSpan.FromMilliseconds(500), stoppingToken);
        }
    }

    private async Task ProcessPendingMessagesAsync(CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<LedgerDbContext>();
        var projectionUpdater = scope.ServiceProvider.GetRequiredService<ProjectionUpdater>();

        var messages = await context.Outbox
            .Where(m => m.ProcessedAt == null)
            .OrderBy(m => m.Timestamp)
            .Take(100)
            .ToListAsync(ct);

        foreach (var message in messages)
        {
            try
            {
                var @event = EventSerializer.Deserialize(message.EventType, message.Payload);
                await projectionUpdater.UpdateAsync(@event, ct);

                message.ProcessedAt = DateTime.UtcNow;
                await context.SaveChangesAsync(ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process outbox message {MessageId}", message.Id);
            }
        }
    }
}
