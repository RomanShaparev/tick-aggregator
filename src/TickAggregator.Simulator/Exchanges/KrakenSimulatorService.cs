using System.Text.Json;

namespace TickAggregator.Simulator.Exchanges;

public sealed class KrakenSimulatorService : ExchangeSimulatorService
{
    private long _tradeId = 59800000;

    public KrakenSimulatorService(ILogger<KrakenSimulatorService> logger) : base(logger) { }

    protected override Task ExecuteAsync(CancellationToken stoppingToken) => Task.CompletedTask;

    public async Task HandleWebSocketAsync(HttpContext context, CancellationToken ct)
    {
        using var ws = await context.WebSockets.AcceptWebSocketAsync();
        Logger.LogInformation("Kraken: client connected");

        await SendTicksAsync(ws, CreateMessage, ct);

        Logger.LogInformation("Kraken: client disconnected");
    }

    private static readonly string[] KrakenSymbols = ["BTC/USD", "ETH/USD", "SOL/USD", "BNB/USD", "XRP/USD"];

    private string CreateMessage()
    {
        var id = Interlocked.Increment(ref _tradeId);
        var (idx, price) = GetRandomTick();
        var volume = Math.Round((decimal)(Rng.NextDouble() * 2 + 0.001), 6);
        var ts = DateTimeOffset.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.ffffffZ");
        var side = Rng.Next(2) == 0 ? "buy" : "sell";

        return JsonSerializer.Serialize(new
        {
            channel = "trade",
            data = new[]
            {
                new
                {
                    trade_id = id,
                    symbol = KrakenSymbols[idx],
                    side,
                    price = (double)price,
                    qty = (double)volume,
                    timestamp = ts
                }
            }
        });
    }
}
