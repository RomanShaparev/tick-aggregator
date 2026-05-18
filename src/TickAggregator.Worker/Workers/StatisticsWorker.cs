using System.Collections.Concurrent;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TickAggregator.Application.Metrics;

namespace TickAggregator.Worker.Workers;

public sealed class StatisticsWorker : BackgroundService
{
    private readonly ILogger<StatisticsWorker> _logger;

    public StatisticsWorker(ILogger<StatisticsWorker> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var countsByExchange = new ConcurrentDictionary<string, long>();

        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, l) =>
        {
            if (instrument.Meter.Name == TickMetrics.MeterName && instrument.Name == "ticks.saved")
                l.EnableMeasurementEvents(instrument);
        };
        listener.SetMeasurementEventCallback<long>((_, value, tags, _) =>
        {
            var exchange = "unknown";
            foreach (var tag in tags)
            {
                if (tag.Key == "exchange") { exchange = tag.Value?.ToString() ?? "unknown"; break; }
            }
            countsByExchange.AddOrUpdate(exchange, value, (_, existing) => existing + value);
        });
        listener.Start();

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            var total = countsByExchange.Values.Sum();
            var byExchange = string.Join(", ", countsByExchange.Select(kvp => $"{kvp.Key}={kvp.Value}"));
            _logger.LogInformation("Ticks saved — total: {Total} | {ByExchange}", total, byExchange);
        }
    }
}
