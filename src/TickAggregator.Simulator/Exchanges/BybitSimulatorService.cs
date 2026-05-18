using System.Text.Json.Nodes;

namespace TickAggregator.Simulator.Exchanges;

public sealed class BybitSimulatorService : ExchangeSimulatorService
{
    public BybitSimulatorService(ILogger<BybitSimulatorService> logger, IConfiguration configuration)
        : base(logger, configuration) { }

    protected override Task ExecuteAsync(CancellationToken stoppingToken) => Task.CompletedTask;

    public async Task HandleWebSocketAsync(HttpContext context, CancellationToken ct)
    {
        using var ws = await context.WebSockets.AcceptWebSocketAsync();
        Logger.LogInformation("Bybit: client connected");

        await SendTicksAsync(ws, CreateMessage, ct);

        Logger.LogInformation("Bybit: client disconnected");
    }

    private string CreateMessage()
    {
        var (idx, price) = GetRandomTick();
        var symbol = Symbols[idx];
        var tradeId = Guid.NewGuid().ToString("N")[..16];
        var volume = Math.Round((decimal)(Rng.NextDouble() * 2 + 0.001), 6);
        var ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var side = Rng.Next(2) == 0 ? "Buy" : "Sell";

        return new JsonObject
        {
            ["topic"] = $"publicTrade.{symbol}",
            ["ts"] = ts,
            ["data"] = new JsonArray(new JsonObject
            {
                ["i"] = tradeId,
                ["T"] = ts,
                ["p"] = price.ToString("F2", System.Globalization.CultureInfo.InvariantCulture),
                ["v"] = volume.ToString("F6", System.Globalization.CultureInfo.InvariantCulture),
                ["S"] = side,
                ["s"] = symbol
            })
        }.ToJsonString();
    }
}
