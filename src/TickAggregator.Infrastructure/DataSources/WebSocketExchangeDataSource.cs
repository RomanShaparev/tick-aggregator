using Microsoft.Extensions.Logging;
using TickAggregator.Domain.Enums;
using TickAggregator.Infrastructure.WebSocket;

namespace TickAggregator.Infrastructure.DataSources;

public sealed class WebSocketExchangeDataSource : IExchangeDataSource
{
    private readonly Uri _uri;
    private readonly int _initialDelayMs;
    private readonly int _maxDelayMs;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<WebSocketExchangeDataSource> _logger;

    public Exchange Exchange { get; }

    public WebSocketExchangeDataSource(
        Exchange exchange,
        Uri uri,
        int initialDelayMs,
        int maxDelayMs,
        ILoggerFactory loggerFactory)
    {
        Exchange = exchange;
        _uri = uri;
        _initialDelayMs = initialDelayMs;
        _maxDelayMs = maxDelayMs;
        _loggerFactory = loggerFactory;
        _logger = loggerFactory.CreateLogger<WebSocketExchangeDataSource>();
    }

    public async IAsyncEnumerable<string> StreamAsync(
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
    {
        var delay = _initialDelayMs;
        var wsLogger = _loggerFactory.CreateLogger<ExchangeWebSocketClient>();
        await using var client = new ExchangeWebSocketClient(Exchange.ToString(), _uri, wsLogger);

        while (!ct.IsCancellationRequested)
        {
            var connected = await TryConnectAsync(client, ct);

            if (connected)
            {
                delay = _initialDelayMs;

                await foreach (var payload in client.ReceiveMessagesAsync(ct))
                    yield return payload;
            }

            if (!ct.IsCancellationRequested)
            {
                await Task.Delay(delay, ct).ContinueWith(_ => { });
                delay = Math.Min(delay * 2, _maxDelayMs);
            }
        }
    }

    private async Task<bool> TryConnectAsync(ExchangeWebSocketClient client, CancellationToken ct)
    {
        try
        {
            client.Reconnect();
            await client.ConnectAsync(ct);
            return true;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to {Exchange}", Exchange);
            return false;
        }
    }
}
