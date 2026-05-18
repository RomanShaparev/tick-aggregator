using TickAggregator.Domain.Entities;
using TickAggregator.Domain.Enums;

namespace TickAggregator.Infrastructure.DataSources.Binance;

public sealed class BinanceTickMapper : ITickMapper<BinanceTick>
{
    public Tick Map(BinanceTick exchangeTick)
    {
        return new Tick
        {
            TradeId = exchangeTick.TradeId.ToString(),
            Exchange = Exchange.Binance,
            Ticker = exchangeTick.Symbol,
            Price = decimal.Parse(exchangeTick.Price, System.Globalization.CultureInfo.InvariantCulture),
            Volume = decimal.Parse(exchangeTick.Quantity, System.Globalization.CultureInfo.InvariantCulture),
            Timestamp = DateTimeOffset.FromUnixTimeMilliseconds(exchangeTick.TradeTimeMs),
        };
    }
}
