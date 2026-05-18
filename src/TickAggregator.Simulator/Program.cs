using TickAggregator.Simulator.Exchanges;

var builder = WebApplication.CreateBuilder(args);

var exchange = builder.Configuration["Simulator:Exchange"];

switch (exchange)
{
    case "Binance": builder.Services.AddSingleton<BinanceSimulatorService>(); break;
    case "Kraken":  builder.Services.AddSingleton<KrakenSimulatorService>();  break;
    case "Bybit":   builder.Services.AddSingleton<BybitSimulatorService>();   break;
    default: throw new NotSupportedException($"Exchange '{exchange}' is not supported.");
}

var app = builder.Build();
app.UseWebSockets();

switch (exchange)
{
    case "Binance":
        app.Map("/ws/binance", async (HttpContext ctx) =>
        {
            if (!ctx.WebSockets.IsWebSocketRequest) { ctx.Response.StatusCode = 400; return; }
            await ctx.RequestServices.GetRequiredService<BinanceSimulatorService>().HandleWebSocketAsync(ctx, ctx.RequestAborted);
        });
        break;
    case "Kraken":
        app.Map("/ws/kraken", async (HttpContext ctx) =>
        {
            if (!ctx.WebSockets.IsWebSocketRequest) { ctx.Response.StatusCode = 400; return; }
            await ctx.RequestServices.GetRequiredService<KrakenSimulatorService>().HandleWebSocketAsync(ctx, ctx.RequestAborted);
        });
        break;
    case "Bybit":
        app.Map("/ws/bybit", async (HttpContext ctx) =>
        {
            if (!ctx.WebSockets.IsWebSocketRequest) { ctx.Response.StatusCode = 400; return; }
            await ctx.RequestServices.GetRequiredService<BybitSimulatorService>().HandleWebSocketAsync(ctx, ctx.RequestAborted);
        });
        break;
}

app.Run("http://0.0.0.0:5100");
