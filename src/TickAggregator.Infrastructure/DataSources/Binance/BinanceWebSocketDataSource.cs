using System.Text.Json;
using Microsoft.Extensions.Logging;
using TickAggregator.Domain.Entities;
using TickAggregator.Domain.Interfaces;
using TickAggregator.Infrastructure.WebSocket;

namespace TickAggregator.Infrastructure.DataSources.Binance;

public sealed class BinanceWebSocketDataSource : IExchangeDataSource
{
    private readonly ExchangeWebSocketClient _client;
    private readonly ILogger<BinanceWebSocketDataSource> _logger;

    public BinanceWebSocketDataSource(Uri uri, TimeSpan initialDelay, TimeSpan maxDelay, ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<BinanceWebSocketDataSource>();
        var wsClientLogger = loggerFactory.CreateLogger($"{nameof(ExchangeWebSocketClient)}.Binance");
        _client = new ExchangeWebSocketClient(uri, initialDelay, maxDelay, wsClientLogger);
    }

    public async IAsyncEnumerable<Tick> StreamAsync(
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
    {
        await foreach (var payload in _client.StreamAsync(ct))
        {
            BinanceTick raw;
            try
            {
                raw = JsonSerializer.Deserialize<BinanceTick>(payload)!;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse Binance message");
                continue;
            }
            yield return raw.ToTick();
        }
    }
}