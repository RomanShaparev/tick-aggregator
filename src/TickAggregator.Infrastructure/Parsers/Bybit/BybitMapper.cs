using TickAggregator.Domain.Entities;
using TickAggregator.Domain.Enums;

namespace TickAggregator.Infrastructure.Parsers.Bybit;

internal static class BybitMapper
{
    internal static Tick ToTick(this BybitTick raw, Exchange exchange) => new()
    {
        TradeId = raw.TradeId,
        Exchange = exchange,
        Ticker = raw.Symbol,
        Price = decimal.Parse(raw.Price, System.Globalization.CultureInfo.InvariantCulture),
        Volume = decimal.Parse(raw.Volume, System.Globalization.CultureInfo.InvariantCulture),
        Timestamp = DateTimeOffset.FromUnixTimeMilliseconds(raw.TradeTimeMs),
        ReceivedAt = DateTimeOffset.UtcNow,
    };
}
