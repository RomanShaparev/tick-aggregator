using Microsoft.Extensions.Logging;
using TickAggregator.Infrastructure.WebSocket;

namespace TickAggregator.Infrastructure.DataSources.Binance;

public sealed class BinanceWebSocketDataSource : ExchangeWebSocketDataSource<BinanceTick>
{
    public BinanceWebSocketDataSource(
        IExchangeWebSocketClient client,
        IParser<BinanceTick> parser,
        IMapper<BinanceTick> mapper,
        ILogger<BinanceWebSocketDataSource> logger)
        : base(client, parser, mapper, logger) { }
}
