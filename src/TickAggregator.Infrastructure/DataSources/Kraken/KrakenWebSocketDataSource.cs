using System.Text.Json;
using Microsoft.Extensions.Logging;
using TickAggregator.Domain.Entities;
using TickAggregator.Domain.Interfaces;
using TickAggregator.Infrastructure.WebSocket;

namespace TickAggregator.Infrastructure.DataSources.Kraken;

public sealed class KrakenWebSocketDataSource : IExchangeDataSource
{
    private readonly ExchangeWebSocketClient _client;
    private readonly ILogger<KrakenWebSocketDataSource> _logger;

    public KrakenWebSocketDataSource(Uri uri, TimeSpan initialDelay, TimeSpan maxDelay, ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<KrakenWebSocketDataSource>();
        var wsClientLogger = loggerFactory.CreateLogger($"{nameof(ExchangeWebSocketClient)}.Kraken");
        _client = new ExchangeWebSocketClient(uri, initialDelay, maxDelay, wsClientLogger);
    }

    public async IAsyncEnumerable<Tick> StreamAsync(
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
    {
        await foreach (var payload in _client.StreamAsync(ct))
        {
            KrakenMessage message;
            try
            {
                message = JsonSerializer.Deserialize<KrakenMessage>(payload)!;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse Kraken message");
                continue;
            }

            if (message.Data is not { Length: > 0 })
                continue;

            foreach (var raw in message.Data)
                yield return raw.ToTick();
        }
    }
}