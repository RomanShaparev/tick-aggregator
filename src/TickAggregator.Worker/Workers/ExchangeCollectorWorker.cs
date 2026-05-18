using TickAggregator.Application.Services;

namespace TickAggregator.Worker.Workers;

public sealed class ExchangeCollectorWorker : BackgroundService
{
    private readonly TickCollectorService _collectorService;

    public ExchangeCollectorWorker(TickCollectorService collectorService)
        => _collectorService = collectorService;

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
        => _collectorService.RunAsync(stoppingToken);
}
