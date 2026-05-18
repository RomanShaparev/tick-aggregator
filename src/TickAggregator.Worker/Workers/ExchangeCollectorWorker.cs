using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TickAggregator.Infrastructure.DataSources;
using TickAggregator.Infrastructure.Messaging;
using TickAggregator.Infrastructure.Parsers;

namespace TickAggregator.Worker.Workers;

public sealed class ExchangeCollectorWorker : BackgroundService
{
    private readonly IEnumerable<IExchangeDataSource> _dataSources;
    private readonly ExchangeParserService _parserService;
    private readonly IMessageProducer _producer;
    private readonly ILogger<ExchangeCollectorWorker> _logger;

    public ExchangeCollectorWorker(
        IEnumerable<IExchangeDataSource> dataSources,
        ExchangeParserService parserService,
        IMessageProducer producer,
        ILogger<ExchangeCollectorWorker> logger)
    {
        _dataSources = dataSources;
        _parserService = parserService;
        _producer = producer;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var tasks = _dataSources.Select(ds => RunAsync(ds, stoppingToken)).ToList();
        await Task.WhenAll(tasks);
    }

    private async Task RunAsync(IExchangeDataSource dataSource, CancellationToken ct)
    {
        _logger.LogInformation("Starting collector for {Exchange}", dataSource.Exchange);

        await foreach (var payload in dataSource.StreamAsync(ct))
        {
            foreach (var tick in _parserService.Parse(dataSource.Exchange, payload))
                await _producer.PublishAsync(tick, ct);
        }

        _logger.LogInformation("Collector stopped for {Exchange}", dataSource.Exchange);
    }
}
