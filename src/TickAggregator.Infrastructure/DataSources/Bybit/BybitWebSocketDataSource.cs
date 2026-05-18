using System.Text.Json;
using Microsoft.Extensions.Logging;
using TickAggregator.Domain.Entities;
using TickAggregator.Domain.Interfaces;
using TickAggregator.Infrastructure.WebSocket;

namespace TickAggregator.Infrastructure.DataSources.Bybit;

public sealed class BybitWebSocketDataSource : IExchangeDataSource
{
    private readonly ExchangeWebSocketClient _client;
    private readonly ILogger<BybitWebSocketDataSource> _logger;

    public BybitWebSocketDataSource(Uri uri, TimeSpan initialDelay, TimeSpan maxDelay, ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<BybitWebSocketDataSource>();
        var wsClientLogger = loggerFactory.CreateLogger($"{nameof(ExchangeWebSocketClient)}.Bybit");
        _client = new ExchangeWebSocketClient(uri, initialDelay, maxDelay, wsClientLogger);
    }

    public async IAsyncEnumerable<Tick> StreamAsync(
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
    {
        await foreach (var payload in _client.StreamAsync(ct))
        {
            BybitMessage message;
            try
            {
                message = JsonSerializer.Deserialize<BybitMessage>(payload)!;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse Bybit message");
                continue;
            }

            if (message.Data is not { Length: > 0 })
                continue;

            foreach (var raw in message.Data)
                yield return raw.ToTick();
        }
    }
}