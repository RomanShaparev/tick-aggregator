using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using TickAggregator.Infrastructure.DataSources.Binance;
using TickAggregator.Infrastructure.DataSources.Bybit;
using TickAggregator.Infrastructure.DataSources.Kraken;

namespace TickAggregator.EndToEndTests.Infrastructure;

public sealed class ExchangeWebSocketServerFixture : IAsyncDisposable
{
    private readonly WebApplication _app;

    public Uri Uri { get; private set; } = null!;

    public ExchangeWebSocketServerFixture(IReadOnlyList<string> messages)
    {
        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        _app = builder.Build();
        _app.UseWebSockets();
        _app.Map("/", async context =>
        {
            if (!context.WebSockets.IsWebSocketRequest)
            {
                context.Response.StatusCode = 400;
                return;
            }

            using var ws = await context.WebSockets.AcceptWebSocketAsync();
            await SendMessagesAsync(ws, messages, context.RequestAborted);
        });
    }

    public async Task StartAsync()
    {
        await _app.StartAsync();
        var port = new Uri(_app.Services.GetRequiredService<IServer>()
            .Features.Get<IServerAddressesFeature>()!.Addresses.First()).Port;
        Uri = new Uri($"ws://localhost:{port}/");
    }

    public async ValueTask DisposeAsync() => await _app.DisposeAsync();

    public static string Binance(long tradeId, string symbol = "BTCUSDT", string price = "50000", string qty = "0.001")
        => JsonSerializer.Serialize(new BinanceTick(tradeId, symbol, price, qty,
            DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()));

    public static string Bybit(string tradeId, string symbol = "BTCUSDT", string price = "50000",
        string volume = "0.001")
        => JsonSerializer.Serialize(new BybitMessage([
            new BybitTick(tradeId, symbol, price, volume, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds())
        ]));

    public static string Kraken(long tradeId, string symbol = "BTC/USD", decimal price = 50000m, decimal qty = 0.001m)
        => JsonSerializer.Serialize(new KrakenMessage([
            new KrakenTick(tradeId, symbol, price, qty, DateTimeOffset.UtcNow)
        ]));

    private static async Task SendMessagesAsync(WebSocket ws, IReadOnlyList<string> messages, CancellationToken ct)
    {
        try
        {
            foreach (var msg in messages)
            {
                if (ws.State != WebSocketState.Open) return;
                await ws.SendAsync(Encoding.UTF8.GetBytes(msg), WebSocketMessageType.Text, endOfMessage: true, ct);
            }

            await Task.Delay(Timeout.Infinite, ct);
        }
        catch (OperationCanceledException)
        {
        }
    }
}