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
    private readonly IParser<T> _parser;
    private readonly IMapper<T> _mapper;
    private readonly IDlqProducer _dlq;
    private readonly Exchange _exchange;
    private readonly ILogger _logger;

    public ExchangeWebSocketDataSource(
        IExchangeWebSocketClient client,
        IParser<T> parser,
        IMapper<T> mapper,
        IDlqProducer dlq,
        Exchange exchange,
        ILogger logger)
    {
        _client = client;
        _parser = parser;
        _mapper = mapper;
        _dlq = dlq;
        _exchange = exchange;
        _logger = logger;
    }

    public async IAsyncEnumerable<Tick> StreamAsync([EnumeratorCancellation] CancellationToken ct)
    {
        await foreach (var payload in _client.StreamAsync(ct))
        {
            if (!_parser.TryParse(payload, out var items))
            {
                _logger.LogWarning("Failed to parse message from {Exchange}, sending to DLQ", _exchange);
                await _dlq.SendAsync(new InvalidMessage(_exchange, payload), ct);
                continue;
            }
            
            foreach (var raw in items)
                yield return _mapper.Map(raw);
        }
    }
}
