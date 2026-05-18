using Microsoft.Extensions.Logging;
using TickAggregator.Infrastructure.WebSocket;

namespace TickAggregator.Infrastructure.DataSources.Kraken;

public sealed class KrakenWebSocketDataSource : ExchangeWebSocketDataSource<KrakenTick>
{
    public KrakenWebSocketDataSource(
        IExchangeWebSocketClient client,
        IParser<KrakenTick> parser,
        IMapper<KrakenTick> mapper,
        ILogger<KrakenWebSocketDataSource> logger)
        : base(client, parser, mapper, logger) { }
}
