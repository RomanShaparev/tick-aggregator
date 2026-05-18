using System.Text.Json;

namespace TickAggregator.Infrastructure.DataSources.Binance;

public sealed class BinanceTickParser : ITickParser<BinanceTick>
{
    public bool TryParse(string payload, out IEnumerable<BinanceTick> items)
    {
        try
        {
            items = [JsonSerializer.Deserialize<BinanceTick>(payload)!];
            return true;
        }
        catch
        {
            items = [];
            return false;
        }
    }
}
