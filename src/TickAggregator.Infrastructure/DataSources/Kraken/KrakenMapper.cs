using TickAggregator.Domain.Entities;
using TickAggregator.Domain.Enums;

namespace TickAggregator.Infrastructure.DataSources.Kraken;

internal static class KrakenMapper
{
    internal static Tick ToTick(this KrakenTick raw) => new()
    {
        TradeId = raw.TradeId.ToString(),
        Exchange = Exchange.Kraken,
        Ticker = raw.Symbol.Replace("/", ""),
        Price = raw.Price,
        Volume = raw.Qty,
        Timestamp = raw.Timestamp,
    };
}
