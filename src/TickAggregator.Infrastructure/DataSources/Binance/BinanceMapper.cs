using TickAggregator.Domain.Entities;
using TickAggregator.Domain.Enums;

namespace TickAggregator.Infrastructure.DataSources.Binance;

public sealed class BinanceMapper : IMapper<BinanceTick>
{
    public Tick Map(BinanceTick raw) => new()
    {
        TradeId = raw.TradeId.ToString(),
        Exchange = Exchange.Binance,
        Ticker = raw.Symbol,
        Price = decimal.Parse(raw.Price, System.Globalization.CultureInfo.InvariantCulture),
        Volume = decimal.Parse(raw.Quantity, System.Globalization.CultureInfo.InvariantCulture),
        Timestamp = DateTimeOffset.FromUnixTimeMilliseconds(raw.TradeTimeMs),
    };
}
