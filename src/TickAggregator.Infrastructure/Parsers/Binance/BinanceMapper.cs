using TickAggregator.Domain.Entities;
using TickAggregator.Domain.Enums;

namespace TickAggregator.Infrastructure.Parsers.Binance;

internal static class BinanceMapper
{
    internal static Tick ToTick(this BinanceTick raw, Exchange exchange) => new()
    {
        TradeId = raw.TradeId.ToString(),
        Exchange = exchange,
        Ticker = raw.Symbol,
        Price = decimal.Parse(raw.Price, System.Globalization.CultureInfo.InvariantCulture),
        Volume = decimal.Parse(raw.Quantity, System.Globalization.CultureInfo.InvariantCulture),
        Timestamp = DateTimeOffset.FromUnixTimeMilliseconds(raw.TradeTimeMs),
        ReceivedAt = DateTimeOffset.UtcNow,
    };
}
