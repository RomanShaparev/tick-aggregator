namespace TickAggregator.Infrastructure.WebSocket;

public interface IExchangeWebSocketClient
{
    IAsyncEnumerable<string> StreamAsync(CancellationToken ct);
}
