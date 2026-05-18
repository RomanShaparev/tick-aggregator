using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;
using TickAggregator.Domain.Entities;
using TickAggregator.Domain.Interfaces;
using TickAggregator.Infrastructure.WebSocket;

namespace TickAggregator.Infrastructure.DataSources;

public class ExchangeWebSocketDataSource<T> : IExchangeDataSource
{
    private readonly IExchangeWebSocketClient _client;
    private readonly IParser<T> _parser;
    private readonly IMapper<T> _mapper;
    private readonly ILogger _logger;

    protected ExchangeWebSocketDataSource(
        IExchangeWebSocketClient client,
        IParser<T> parser,
        IMapper<T> mapper,
        ILogger logger)
    {
        _client = client;
        _parser = parser;
        _mapper = mapper;
        _logger = logger;
    }

    public async IAsyncEnumerable<Tick> StreamAsync([EnumeratorCancellation] CancellationToken ct)
    {
        await foreach (var payload in _client.StreamAsync(ct))
        {
            IEnumerable<T> items;
            try
            {
                items = _parser.Parse(payload);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse message");
                continue;
            }
            foreach (var raw in items)
                yield return _mapper.Map(raw);
        }
    }
}
