using System.Diagnostics.Metrics;

namespace TickAggregator.Application.Metrics;

public sealed class TickMetrics : IDisposable
{
    public const string MeterName = "TickAggregator";

    private readonly Meter _meter;
    public Counter<long> TicksSaved { get; }

    public TickMetrics()
    {
        _meter = new Meter(MeterName);
        TicksSaved = _meter.CreateCounter<long>("ticks.saved", "ticks", "Ticks successfully written to the database");
    }

    public void Dispose() => _meter.Dispose();
}
