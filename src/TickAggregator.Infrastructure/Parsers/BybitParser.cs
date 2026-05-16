using System.Text.Json;
using TickAggregator.Domain.Entities;
using TickAggregator.Domain.Interfaces;

namespace TickAggregator.Infrastructure.Parsers;

public sealed class BybitParser : IExchangeParser
{
    public string ExchangeName => "Bybit";

    public IReadOnlyList<Tick> Parse(string rawMessage)
    {
        var doc = JsonDocument.Parse(rawMessage);
        var root = doc.RootElement;

        if (!root.TryGetProperty("data", out var dataArray))
            return [];

        var result = new List<Tick>();
        foreach (var item in dataArray.EnumerateArray())
        {
            var tradeId = item.GetProperty("i").GetString()!;
            var symbol = item.GetProperty("s").GetString()!;
            var price = decimal.Parse(item.GetProperty("p").GetString()!,
                System.Globalization.CultureInfo.InvariantCulture);
            var volume = decimal.Parse(item.GetProperty("v").GetString()!,
                System.Globalization.CultureInfo.InvariantCulture);
            var tradeTimeMs = item.GetProperty("T").GetInt64();
            var timestamp = DateTimeOffset.FromUnixTimeMilliseconds(tradeTimeMs);

            result.Add(new Tick
            {
                TradeId = tradeId,
                Exchange = ExchangeName,
                Ticker = symbol,
                Price = price,
                Volume = volume,
                Timestamp = timestamp,
                ReceivedAt = DateTimeOffset.UtcNow,
            });
        }
        return result;
    }
}
