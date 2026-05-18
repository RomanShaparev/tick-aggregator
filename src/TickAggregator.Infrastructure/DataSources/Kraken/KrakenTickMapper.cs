using TickAggregator.Domain.Entities;
using TickAggregator.Domain.Enums;

namespace TickAggregator.Infrastructure.DataSources.Kraken;

public sealed class KrakenTickMapper : ITickMapper<KrakenTick>
{
    public Tick Map(KrakenTick exchangeTick)
    {
        return new Tick
        {
            TradeId = exchangeTick.TradeId.ToString(),
            Exchange = Exchange.Kraken,
            Ticker = exchangeTick.Symbol.Replace("/", ""),
            Price = exchangeTick.Price,
            Volume = exchangeTick.Qty,
            Timestamp = exchangeTick.Timestamp,
        };
    }
}
