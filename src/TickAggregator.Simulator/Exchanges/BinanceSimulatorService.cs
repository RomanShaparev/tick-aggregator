using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace TickAggregator.Simulator.Exchanges;

public sealed class BinanceSimulatorService : ExchangeSimulatorService
{
    private long _tradeId = 10000000;

    public BinanceSimulatorService(ILogger<BinanceSimulatorService> logger) : base(logger) { }

    protected override Task ExecuteAsync(CancellationToken stoppingToken) => Task.CompletedTask;

    public async Task HandleWebSocketAsync(HttpContext context, CancellationToken ct)
    {
        using var ws = await context.WebSockets.AcceptWebSocketAsync();
        Logger.LogInformation("Binance: client connected");

        await SendTicksAsync(ws, CreateMessage, ct);

        Logger.LogInformation("Binance: client disconnected");
    }

    private string CreateMessage()
    {
        var id = Interlocked.Increment(ref _tradeId);
        var (idx, price) = GetRandomTick();
        var volume = Math.Round((decimal)(Rng.NextDouble() * 2 + 0.001), 6);
        var ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        return JsonSerializer.Serialize(new
        {
            e = "trade",
            t = id,
            s = Symbols[idx],
            p = price.ToString("F2"),
            q = volume.ToString("F6"),
            T = ts
        });
    }
}
