using TickAggregator.Domain.Entities;
using TickAggregator.Domain.Enums;

namespace TickAggregator.Infrastructure.DataSources.Kraken;

public sealed class KrakenMapper : IMapper<KrakenTick>
{
    public Tick Map(KrakenTick raw) => new()
    {
        TradeId = raw.TradeId.ToString(),
        Exchange = Exchange.Kraken,
        Ticker = raw.Symbol.Replace("/", ""),
        Price = raw.Price,
        Volume = raw.Qty,
        Timestamp = raw.Timestamp,
    };
}
