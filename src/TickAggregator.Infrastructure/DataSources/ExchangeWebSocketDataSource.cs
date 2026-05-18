using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;
using TickAggregator.Domain.Entities;
using TickAggregator.Domain.Enums;
using TickAggregator.Domain.Interfaces;
using TickAggregator.Infrastructure.Messaging;
using TickAggregator.Infrastructure.WebSocket;

namespace TickAggregator.Infrastructure.DataSources;

public class ExchangeWebSocketDataSource<T> : IExchangeDataSource
{
    private readonly IExchangeWebSocketClient _client;
    private readonly ITickParser<T> _tickParser;
    private readonly ITickMapper<T> _tickMapper;
    private readonly IDlqProducer _dlq;
    private readonly Exchange _exchange;
    private readonly ILogger _logger;

    public ExchangeWebSocketDataSource(
        IExchangeWebSocketClient client,
        ITickParser<T> tickParser,
        ITickMapper<T> tickMapper,
        IDlqProducer dlq,
        Exchange exchange,
        ILogger logger)
    {
        _client = client;
        _tickParser = tickParser;
        _tickMapper = tickMapper;
        _dlq = dlq;
        _exchange = exchange;
        _logger = logger;
    }

    public async IAsyncEnumerable<Tick> StreamAsync([EnumeratorCancellation] CancellationToken ct)
    {
        await foreach (var payload in _client.StreamAsync(ct))
        {
            if (!_tickParser.TryParse(payload, out var items))
            {
                _logger.LogWarning("Failed to parse message from {Exchange}, sending to DLQ", _exchange);
                await _dlq.SendAsync(new InvalidMessage(_exchange, payload), ct);
                continue;
            }
            
            foreach (var raw in items)
                yield return _tickMapper.Map(raw);
        }
    }
}
