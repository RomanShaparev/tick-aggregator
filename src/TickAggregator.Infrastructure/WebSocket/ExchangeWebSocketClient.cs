using System.Net.WebSockets;
using System.Text;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;

namespace TickAggregator.Infrastructure.WebSocket;

public sealed class ExchangeWebSocketClient : IExchangeWebSocketClient
{
    private readonly Uri _uri;
    private readonly ResiliencePipeline _pipeline;
    private readonly ILogger _logger;
    private const int ReceiveBufferSize = 16384;

    public ExchangeWebSocketClient(
        Uri uri,
        TimeSpan initialDelay,
        TimeSpan maxDelay,
        ILogger logger)
    {
        _uri = uri;
        _logger = logger;
        _pipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = int.MaxValue,
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                Delay = initialDelay,
                MaxDelay = maxDelay,
                ShouldHandle = new PredicateBuilder().Handle<Exception>(ex => ex is not OperationCanceledException),
                OnRetry = args =>
                {
                    _logger.LogWarning("Reconnecting to {Uri} in {Delay}s (attempt #{Attempt})",
                        _uri, args.RetryDelay.TotalSeconds.ToString("F1"), args.AttemptNumber + 1);
                    return ValueTask.CompletedTask;
                }
            })
            .Build();
    }

    public IAsyncEnumerable<string> StreamAsync(CancellationToken ct)
    {
        var channel = Channel.CreateUnbounded<string>(new UnboundedChannelOptions { SingleReader = true });
        _ = ProduceAsync(channel.Writer, ct);
        return channel.Reader.ReadAllAsync(ct);
    }

    private async Task ProduceAsync(ChannelWriter<string> writer, CancellationToken ct)
    {
        try
        {
            await _pipeline.ExecuteAsync(async innerCt =>
            {
                using var ws = new ClientWebSocket();

                _logger.LogInformation("Connecting to {Uri}", _uri);
                await ws.ConnectAsync(_uri, innerCt);
                _logger.LogInformation("Connected to {Uri}", _uri);

                await foreach (var msg in ReceiveAsync(ws, innerCt))
                    await writer.WriteAsync(msg, innerCt);
            }, ct);
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            writer.Complete();
        }
    }

    private async IAsyncEnumerable<string> ReceiveAsync(
        ClientWebSocket ws,
        [System.Runtime.CompilerServices.EnumeratorCancellation]
        CancellationToken ct)
    {
        var buffer = new byte[ReceiveBufferSize];
        var sb = new StringBuilder();

        while (!ct.IsCancellationRequested)
        {
            WebSocketReceiveResult result;
            try
            {
                result = await ws.ReceiveAsync(buffer, ct);
            }
            catch (Exception ex) when (!ct.IsCancellationRequested)
            {
                _logger.LogError(ex, "Receive error from {Uri}", _uri);
                throw;
            }

            if (result.MessageType == WebSocketMessageType.Close)
            {
                _logger.LogWarning("WebSocket closed by {Uri}", _uri);
                throw new WebSocketException(WebSocketError.ConnectionClosedPrematurely, "Server closed connection");
            }

            sb.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));

            if (result.EndOfMessage)
            {
                var message = sb.ToString();
                sb.Clear();
                if (!string.IsNullOrWhiteSpace(message))
                    yield return message;
            }
        }
    }
}