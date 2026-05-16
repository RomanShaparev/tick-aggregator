using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TickAggregator.Application.Interfaces;
using TickAggregator.Application.Messages;
using TickAggregator.Infrastructure.WebSocket;
using TickAggregator.Worker.Configuration;

namespace TickAggregator.Worker.Workers;

public sealed class ExchangeCollectorWorker : BackgroundService
{
    private readonly ExchangeOptions _options;
    private readonly IMessageProducer _producer;
    private readonly ILogger<ExchangeCollectorWorker> _logger;
    private readonly ILoggerFactory _loggerFactory;

    public ExchangeCollectorWorker(
        IOptions<ExchangeOptions> options,
        IMessageProducer producer,
        ILogger<ExchangeCollectorWorker> logger,
        ILoggerFactory loggerFactory)
    {
        _options = options.Value;
        _producer = producer;
        _logger = logger;
        _loggerFactory = loggerFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var tasks = _options.Items.Select(cfg =>
            RunExchangeLoopAsync(cfg, stoppingToken)).ToList();

        await Task.WhenAll(tasks);
    }

    private async Task RunExchangeLoopAsync(ExchangeConfig cfg, CancellationToken ct)
    {
        var wsLogger = _loggerFactory.CreateLogger<ExchangeWebSocketClient>();
        await using var client = new ExchangeWebSocketClient(cfg.Name, new Uri(cfg.WebSocketUrl), wsLogger);

        var delay = cfg.ReconnectDelayMs;

        while (!ct.IsCancellationRequested)
        {
            try
            {
                client.Reconnect();
                await client.ConnectAsync(ct);
                delay = cfg.ReconnectDelayMs;

                await foreach (var message in client.ReceiveMessagesAsync(ct))
                {
                    await _producer.PublishAsync(
                        new RawTickMessage { Exchange = cfg.Name, Payload = message }, ct);
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Connection error for {Exchange}, reconnecting in {Delay}ms", cfg.Name, delay);
            }

            if (!ct.IsCancellationRequested)
            {
                await Task.Delay(delay, ct).ContinueWith(_ => { });
                delay = Math.Min(delay * 2, cfg.MaxReconnectDelayMs);
            }
        }

        _logger.LogInformation("Collector stopped for {Exchange}", cfg.Name);
    }
}
