using Microsoft.Extensions.Logging;
using TickAggregator.Application.Interfaces;
using TickAggregator.Domain.Interfaces;

namespace TickAggregator.Application.Services;

public sealed class TickCollectorService
{
    private readonly IEnumerable<IExchangeDataSource> _dataSources;
    private readonly ITickProducer _producer;
    private readonly ILogger<TickCollectorService> _logger;

    public TickCollectorService(
        IEnumerable<IExchangeDataSource> dataSources,
        ITickProducer producer,
        ILogger<TickCollectorService> logger)
    {
        _dataSources = dataSources;
        _producer = producer;
        _logger = logger;
    }

    public Task RunAsync(CancellationToken ct)
        => Task.WhenAll(_dataSources.Select(ds => CollectFromSourceAsync(ds, ct)));

    private async Task CollectFromSourceAsync(IExchangeDataSource source, CancellationToken ct)
    {
        _logger.LogInformation("Starting collector for {DataSource}", source.GetType().Name);

        try
        {
            await foreach (var tick in source.StreamAsync(ct))
                await _producer.PublishAsync(tick, ct);
        }
        catch (OperationCanceledException)
        {
        }

        _logger.LogInformation("Collector stopped for {DataSource}", source.GetType().Name);
    }
}
