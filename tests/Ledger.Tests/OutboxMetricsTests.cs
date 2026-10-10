using System.Diagnostics.Metrics;
using Ledger.Infrastructure.Outbox;

namespace Ledger.Tests;

public class OutboxMetricsTests
{
    [Fact]
    public void RecordsOutboxCountersDurationAndPendingMessages()
    {
        var measurements = new Dictionary<string, List<double>>();
        using var listener = new MeterListener
        {
            InstrumentPublished = (instrument, meterListener) =>
            {
                if (instrument.Meter.Name == OutboxMetrics.MeterName)
                {
                    meterListener.EnableMeasurementEvents(instrument);
                }
            }
        };

        listener.SetMeasurementEventCallback<long>((instrument, value, _, _) =>
            Record(instrument.Name, value));
        listener.SetMeasurementEventCallback<double>((instrument, value, _, _) =>
            Record(instrument.Name, value));
        listener.Start();

        using var metrics = new OutboxMetrics();

        metrics.RecordEnqueued(2);
        metrics.SetPending(3);
        metrics.RecordProcessed(TimeSpan.FromMilliseconds(25));
        metrics.RecordFailed(TimeSpan.FromMilliseconds(50));
        listener.RecordObservableInstruments();

        Assert.Equal(2, measurements["ledger.outbox.messages.enqueued"].Single());
        Assert.Equal(1, measurements["ledger.outbox.messages.processed"].Single());
        Assert.Equal(1, measurements["ledger.outbox.messages.failed"].Single());
        Assert.Equal(0.025, measurements["ledger.outbox.message.processing.duration"][0], 3);
        Assert.Equal(0.05, measurements["ledger.outbox.message.processing.duration"][1], 3);
        Assert.Equal(2, measurements["ledger.outbox.messages.pending"].Single());

        void Record(string name, double value)
        {
            if (!measurements.TryGetValue(name, out var values))
            {
                values = [];
                measurements.Add(name, values);
            }

            values.Add(value);
        }
    }
}
