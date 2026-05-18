using TickAggregator.Domain.Entities;
using TickAggregator.Domain.Enums;

namespace TickAggregator.Infrastructure.DataSources.Bybit;

public sealed class BybitTickMapper : ITickMapper<BybitTick>
{
    public Tick Map(BybitTick exchangeTick)
    {
        return new Tick
        {
            TradeId = exchangeTick.TradeId,
            Exchange = Exchange.Bybit,
            Ticker = exchangeTick.Symbol,
            Price = decimal.Parse(exchangeTick.Price, System.Globalization.CultureInfo.InvariantCulture),
            Volume = decimal.Parse(exchangeTick.Volume, System.Globalization.CultureInfo.InvariantCulture),
            Timestamp = DateTimeOffset.FromUnixTimeMilliseconds(exchangeTick.TradeTimeMs),
        };
    }
}
