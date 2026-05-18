using System.Text.Json;

namespace TickAggregator.Infrastructure.DataSources.Binance;

public sealed class BinanceParser : IParser<BinanceTick>
{
    public IEnumerable<BinanceTick> Parse(string payload)
        => [JsonSerializer.Deserialize<BinanceTick>(payload)!];
}
