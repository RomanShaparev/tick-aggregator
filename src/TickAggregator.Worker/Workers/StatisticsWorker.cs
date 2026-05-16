using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TickAggregator.Application.Interfaces;

namespace TickAggregator.Worker.Workers;

public sealed class StatisticsWorker : BackgroundService
{
    private readonly ITickCounter _counter;
    private readonly ILogger<StatisticsWorker> _logger;

    public StatisticsWorker(ITickCounter counter, ILogger<StatisticsWorker> logger)
    {
        _counter = counter;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(10));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            _logger.LogInformation("Ticks processed — total: {Total} | {ByExchange}",
                _counter.GetTotal(),
                string.Join(", ", _counter.GetByExchange().Select(kvp => $"{kvp.Key}={kvp.Value}")));
        }
    }
}
