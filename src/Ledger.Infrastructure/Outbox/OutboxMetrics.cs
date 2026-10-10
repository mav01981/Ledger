using System.Diagnostics.Metrics;

namespace Ledger.Infrastructure.Outbox;

public sealed class OutboxMetrics : IDisposable
{
    public const string MeterName = "Ledger.Infrastructure.Outbox";

    private readonly Meter _meter = new(MeterName);
    private readonly Counter<long> _messagesEnqueued;
    private readonly Counter<long> _messagesProcessed;
    private readonly Counter<long> _messagesFailed;
    private readonly Histogram<double> _processingDuration;
    private long _pendingMessages;

    public OutboxMetrics()
    {
        _messagesEnqueued = _meter.CreateCounter<long>(
            "ledger.outbox.messages.enqueued",
            "{message}",
            "Number of messages added to the outbox.");
        _messagesProcessed = _meter.CreateCounter<long>(
            "ledger.outbox.messages.processed",
            "{message}",
            "Number of outbox messages processed successfully.");
        _messagesFailed = _meter.CreateCounter<long>(
            "ledger.outbox.messages.failed",
            "{message}",
            "Number of failed outbox message processing attempts.");
        _processingDuration = _meter.CreateHistogram<double>(
            "ledger.outbox.message.processing.duration",
            "s",
            "Time spent processing an outbox message.");
        _meter.CreateObservableGauge(
            "ledger.outbox.messages.pending",
            () => Interlocked.Read(ref _pendingMessages),
            "{message}",
            "Current number of unprocessed outbox messages.");
    }

    public void RecordEnqueued(long count)
    {
        if (count > 0)
        {
            _messagesEnqueued.Add(count);
        }
    }

    public void RecordProcessed(TimeSpan duration)
    {
        _messagesProcessed.Add(1);
        _processingDuration.Record(duration.TotalSeconds);
        Interlocked.Decrement(ref _pendingMessages);
    }

    public void RecordFailed(TimeSpan duration)
    {
        _messagesFailed.Add(1);
        _processingDuration.Record(duration.TotalSeconds);
    }

    public void SetPending(long count) =>
        Interlocked.Exchange(ref _pendingMessages, count);

    public void Dispose() => _meter.Dispose();
}
