using TickAggregator.Simulator.Exchanges;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<BinanceSimulatorService>();
builder.Services.AddSingleton<KrakenSimulatorService>();
builder.Services.AddSingleton<BybitSimulatorService>();

var app = builder.Build();

app.UseWebSockets();

app.Map("/ws/binance", async (HttpContext ctx) =>
{
    if (!ctx.WebSockets.IsWebSocketRequest) { ctx.Response.StatusCode = 400; return; }
    var svc = ctx.RequestServices.GetRequiredService<BinanceSimulatorService>();
    await svc.HandleWebSocketAsync(ctx, ctx.RequestAborted);
});

app.Map("/ws/kraken", async (HttpContext ctx) =>
{
    if (!ctx.WebSockets.IsWebSocketRequest) { ctx.Response.StatusCode = 400; return; }
    var svc = ctx.RequestServices.GetRequiredService<KrakenSimulatorService>();
    await svc.HandleWebSocketAsync(ctx, ctx.RequestAborted);
});

app.Map("/ws/bybit", async (HttpContext ctx) =>
{
    if (!ctx.WebSockets.IsWebSocketRequest) { ctx.Response.StatusCode = 400; return; }
    var svc = ctx.RequestServices.GetRequiredService<BybitSimulatorService>();
    await svc.HandleWebSocketAsync(ctx, ctx.RequestAborted);
});

app.Run("http://0.0.0.0:5100");
