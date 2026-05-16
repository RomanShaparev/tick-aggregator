using System.Net.WebSockets;
using System.Text;
using Microsoft.Extensions.Logging;

namespace TickAggregator.Infrastructure.WebSocket;

public sealed class ExchangeWebSocketClient : IAsyncDisposable
{
    private ClientWebSocket _ws = new();
    private readonly ILogger<ExchangeWebSocketClient> _logger;
    private readonly string _exchangeName;
    private readonly Uri _uri;

    public string ExchangeName => _exchangeName;

    public ExchangeWebSocketClient(string exchangeName, Uri uri, ILogger<ExchangeWebSocketClient> logger)
    {
        _exchangeName = exchangeName;
        _uri = uri;
        _logger = logger;
    }

    public async Task ConnectAsync(CancellationToken ct)
    {
        _logger.LogInformation("Connecting to {Exchange} WebSocket at {Uri}", _exchangeName, _uri);
        await _ws.ConnectAsync(_uri, ct);
        _logger.LogInformation("Connected to {Exchange}", _exchangeName);
    }

    public async IAsyncEnumerable<string> ReceiveMessagesAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
    {
        var buffer = new byte[16384];
        var messageBuffer = new StringBuilder();

        while (!ct.IsCancellationRequested && _ws.State == WebSocketState.Open)
        {
            WebSocketReceiveResult result;
            try
            {
                result = await _ws.ReceiveAsync(buffer, ct);
            }
            catch (Exception ex) when (!ct.IsCancellationRequested)
            {
                _logger.LogError(ex, "WebSocket receive error from {Exchange}", _exchangeName);
                yield break;
            }

            if (result.MessageType == WebSocketMessageType.Close)
            {
                _logger.LogWarning("WebSocket closed by server for {Exchange}", _exchangeName);
                yield break;
            }

            messageBuffer.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));

            if (result.EndOfMessage)
            {
                var message = messageBuffer.ToString();
                messageBuffer.Clear();
                if (!string.IsNullOrWhiteSpace(message))
                    yield return message;
            }
        }
    }

    public bool IsConnected => _ws.State == WebSocketState.Open;

    public async Task DisconnectAsync()
    {
        if (_ws.State == WebSocketState.Open)
        {
            try
            {
                await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
            }
            catch { /* ignore */ }
        }
        _logger.LogInformation("Disconnected from {Exchange}", _exchangeName);
    }

    public void Reconnect()
    {
        _ws.Dispose();
        _ws = new ClientWebSocket();
    }

    public async ValueTask DisposeAsync()
    {
        await DisconnectAsync();
        _ws.Dispose();
    }
}
