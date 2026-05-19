using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using System.Net.WebSockets;
using System.Text;
using TickAggregator.Infrastructure.WebSocket;
using TickAggregator.IntegrationTests.Infrastructure;
using Xunit;

namespace TickAggregator.IntegrationTests;

public sealed class ExchangeWebSocketClientTests : IAsyncLifetime
{
    private readonly TimeSpan _clientStreamingTimeOut = TimeSpan.FromSeconds(5);
    private readonly WebSocketServerFixture _server = new();

    public Task InitializeAsync() => _server.InitializeAsync();
    public Task DisposeAsync() => _server.DisposeAsync();

    private ExchangeWebSocketClient CreateClient(int bufferSize = 100)
    {
        return new ExchangeWebSocketClient(
            _server.Uri,
            TimeSpan.FromMilliseconds(1),
            TimeSpan.FromMilliseconds(10),
            bufferSize,
            NullLogger.Instance);
    }

    private Task SendWsTextAsync(WebSocket ws, string text, bool endOfMessage, CancellationToken ct)
    {
        var bytes = Encoding.UTF8.GetBytes(text);
        return ws.SendAsync(bytes, WebSocketMessageType.Text, endOfMessage, cancellationToken: ct);
    }

    private Task CloseWsAsync(WebSocket ws, CancellationToken ct)
    {
        return ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "done", ct);
    }

    [Fact]
    public async Task StreamAsync_ServerSendsMessages_AllReceived()
    {
        _server.EnqueueHandler(async (ws, ct) =>
        {
            await SendWsTextAsync(ws, "msg1", true, ct);
            await SendWsTextAsync(ws, "msg2", true, ct);
            await SendWsTextAsync(ws, "msg3", true, ct);
            await Task.Delay(Timeout.Infinite, ct).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
        });

        var client = CreateClient();
        using var cts = new CancellationTokenSource(_clientStreamingTimeOut);
        var messages = new List<string>();

        try
        {
            await foreach (var msg in client.StreamAsync(cts.Token))
            {
                messages.Add(msg);
                if (messages.Count == 3)
                    await cts.CancelAsync();
            }
        }
        catch (OperationCanceledException)
        {
        }

        messages.Should().Equal("msg1", "msg2", "msg3");
    }

    [Fact]
    public async Task StreamAsync_MultiFrameMessage_AssembledAsOneString()
    {
        _server.EnqueueHandler(async (ws, ct) =>
        {
            await SendWsTextAsync(ws, "hello", false, ct);
            await SendWsTextAsync(ws, " world", true, ct);
            await Task.Delay(Timeout.Infinite, ct).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
        });

        var client = CreateClient();
        using var cts = new CancellationTokenSource(_clientStreamingTimeOut);
        var messages = new List<string>();

        try
        {
            await foreach (var msg in client.StreamAsync(cts.Token))
            {
                messages.Add(msg);
                await cts.CancelAsync();
            }
        }
        catch (OperationCanceledException)
        {
        }

        messages.Should().ContainSingle().Which.Should().Be("hello world");
    }

    [Fact]
    public async Task StreamAsync_ServerClosesConnection_ClientReconnects()
    {
        _server.EnqueueHandler(async (ws, ct) =>
        {
            await SendWsTextAsync(ws, "before-disconnect", true, ct);
            await CloseWsAsync(ws, ct);
        });

        _server.EnqueueHandler(async (ws, ct) =>
        {
            await SendWsTextAsync(ws, "after-reconnect", true, ct);
            await Task.Delay(Timeout.Infinite, ct).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
        });

        var client = CreateClient();
        using var cts = new CancellationTokenSource(_clientStreamingTimeOut);
        var messages = new List<string>();

        try
        {
            await foreach (var msg in client.StreamAsync(cts.Token))
            {
                messages.Add(msg);
                if (messages.Count == 2)
                    await cts.CancelAsync();
            }
        }
        catch (OperationCanceledException)
        {
        }

        messages.Should().Equal("before-disconnect", "after-reconnect");
    }

    [Fact]
    public async Task StreamAsync_EmptyAndWhitespaceMessages_Skipped()
    {
        _server.EnqueueHandler(async (ws, ct) =>
        {
            await SendWsTextAsync(ws, "", true, ct);
            await SendWsTextAsync(ws, "   ", true, ct);
            await SendWsTextAsync(ws, "valid", true, ct);
            await Task.Delay(Timeout.Infinite, ct).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
        });

        var client = CreateClient();
        using var cts = new CancellationTokenSource(_clientStreamingTimeOut);
        var messages = new List<string>();

        try
        {
            await foreach (var msg in client.StreamAsync(cts.Token))
            {
                messages.Add(msg);
                await cts.CancelAsync();
            }
        }
        catch (OperationCanceledException)
        {
        }

        messages.Should().ContainSingle().Which.Should().Be("valid");
    }
}