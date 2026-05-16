using System.Text.Json;
using TickAggregator.Domain.Entities;
using TickAggregator.Domain.Interfaces;

namespace TickAggregator.Infrastructure.Parsers;

public sealed class KrakenParser : IExchangeParser
{
    public string ExchangeName => "Kraken";

    public IReadOnlyList<Tick> Parse(string rawMessage)
    {
        var doc = JsonDocument.Parse(rawMessage);
        var root = doc.RootElement;

        if (!root.TryGetProperty("data", out var dataArray))
            return [];

        var result = new List<Tick>();
        foreach (var item in dataArray.EnumerateArray())
        {
            var tradeId = item.GetProperty("trade_id").GetInt64().ToString();
            var symbol = item.GetProperty("symbol").GetString()!;
            var price = item.GetProperty("price").GetDecimal();
            var qty = item.GetProperty("qty").GetDecimal();
            var timestamp = item.GetProperty("timestamp").GetDateTimeOffset();

            result.Add(new Tick
            {
                TradeId = tradeId,
                Exchange = ExchangeName,
                Ticker = symbol.Replace("/", ""),
                Price = price,
                Volume = qty,
                Timestamp = timestamp,
                ReceivedAt = DateTimeOffset.UtcNow,
            });
        }
        return result;
    }
}
