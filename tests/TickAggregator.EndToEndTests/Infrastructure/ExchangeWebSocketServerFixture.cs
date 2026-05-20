using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using TickAggregator.Domain.Entities;
using TickAggregator.Domain.Enums;
using TickAggregator.Infrastructure.DataSources.Binance;
using TickAggregator.Infrastructure.DataSources.Bybit;
using TickAggregator.Infrastructure.DataSources.Kraken;

namespace TickAggregator.EndToEndTests.Infrastructure;

public sealed class ExchangeWebSocketServerFixture : IAsyncDisposable
{
    private readonly WebApplication _app;

    public Uri Uri { get; private set; } = null!;

    public ExchangeWebSocketServerFixture(IReadOnlyList<Tick> ticks)
    {
        var messages = ticks.Select(Serialize).ToList();

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

    private static string Serialize(Tick tick) => tick.Exchange switch
    {
        Exchange.Binance => JsonSerializer.Serialize(new BinanceTick(
            long.Parse(tick.TradeId, CultureInfo.InvariantCulture),
            tick.Ticker,
            tick.Price.ToString(CultureInfo.InvariantCulture),
            tick.Volume.ToString(CultureInfo.InvariantCulture),
            tick.Timestamp.ToUnixTimeMilliseconds())),
        Exchange.Bybit => JsonSerializer.Serialize(new BybitMessage([
            new BybitTick(
                tick.TradeId,
                tick.Ticker,
                tick.Price.ToString(CultureInfo.InvariantCulture),
                tick.Volume.ToString(CultureInfo.InvariantCulture),
                tick.Timestamp.ToUnixTimeMilliseconds())
        ])),
        Exchange.Kraken => JsonSerializer.Serialize(new KrakenMessage([
            new KrakenTick(
                long.Parse(tick.TradeId, CultureInfo.InvariantCulture),
                tick.Ticker,
                tick.Price,
                tick.Volume,
                tick.Timestamp)
        ])),
        _ => throw new ArgumentOutOfRangeException(nameof(tick), tick.Exchange, "Unsupported exchange."),
    };

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
