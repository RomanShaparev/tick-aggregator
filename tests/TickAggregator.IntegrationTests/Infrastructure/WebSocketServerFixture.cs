using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using Xunit;

namespace TickAggregator.IntegrationTests.Infrastructure;

public sealed class WebSocketServerFixture : IAsyncLifetime
{
    private readonly WebApplication _app;
    private readonly ConcurrentQueue<Func<WebSocket, CancellationToken, Task>> _handlers = new();

    public Uri Uri { get; private set; } = null!;

    public WebSocketServerFixture()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        _app = builder.Build();
        _app.UseWebSockets();
        _app.Map("/ws", async context =>
        {
            if (!context.WebSockets.IsWebSocketRequest)
            {
                context.Response.StatusCode = 400;
                return;
            }
            
            using var ws = await context.WebSockets.AcceptWebSocketAsync();
            if (_handlers.TryDequeue(out var handler))
                await handler(ws, context.RequestAborted);
        });
    }

    public async Task InitializeAsync()
    {
        await _app.StartAsync();
        var addresses = _app.Services.GetRequiredService<IServer>()
            .Features.Get<IServerAddressesFeature>()!;
        var port = new Uri(addresses.Addresses.First()).Port;
        Uri = new Uri($"ws://localhost:{port}/ws");
    }

    public async Task DisposeAsync() => await _app.DisposeAsync();

    public void EnqueueHandler(Func<WebSocket, CancellationToken, Task> handler)
        => _handlers.Enqueue(handler);

}
