using Microsoft.Extensions.Logging;
using TickAggregator.Infrastructure.WebSocket;

namespace TickAggregator.Infrastructure.DataSources.Bybit;

public sealed class BybitWebSocketDataSource : ExchangeWebSocketDataSource<BybitTick>
{
    public BybitWebSocketDataSource(
        IExchangeWebSocketClient client,
        IParser<BybitTick> parser,
        IMapper<BybitTick> mapper,
        ILogger<BybitWebSocketDataSource> logger)
        : base(client, parser, mapper, logger) { }
}
