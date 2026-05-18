using TickAggregator.Domain.Entities;
using TickAggregator.Domain.Enums;

namespace TickAggregator.Infrastructure.Parsers.Kraken;

internal static class KrakenMapper
{
    internal static Tick ToTick(this KrakenTick raw, Exchange exchange) => new()
    {
        TradeId = raw.TradeId.ToString(),
        Exchange = exchange,
        Ticker = raw.Symbol.Replace("/", ""),
        Price = raw.Price,
        Volume = raw.Qty,
        Timestamp = raw.Timestamp,
        ReceivedAt = DateTimeOffset.UtcNow,
    };
}
