using System.Net.WebSockets;
using System.Text;

namespace TickAggregator.Simulator;

public abstract class ExchangeSimulatorService : BackgroundService
{
    protected readonly ILogger Logger;
    protected static readonly Random Rng = new();

    protected static readonly string[] Symbols = ["BTCUSDT", "ETHUSDT", "SOLUSDT", "BNBUSDT", "XRPUSDT"];
    protected static readonly decimal[] BasePrices = [50000m, 3000m, 150m, 400m, 0.6m];

    protected ExchangeSimulatorService(ILogger logger) => Logger = logger;

    protected async Task SendTicksAsync(
        System.Net.WebSockets.WebSocket ws,
        Func<string> messageFactory,
        CancellationToken ct)
    {
        while (!ct.IsCancellationRequested && ws.State == WebSocketState.Open)
        {
            try
            {
                var message = messageFactory();
                var bytes = Encoding.UTF8.GetBytes(message);
                await ws.SendAsync(bytes, WebSocketMessageType.Text, true, ct);
                await Task.Delay(Rng.Next(10, 50), ct); // 20-100 тиков/сек
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error sending tick");
                break;
            }
        }
    }

    protected static (int symbolIdx, decimal price) GetRandomTick()
    {
        var idx = Rng.Next(Symbols.Length);
        var price = BasePrices[idx] * (1 + (decimal)(Rng.NextDouble() - 0.5) * 0.02m);
        return (idx, Math.Round(price, 2));
    }
}
