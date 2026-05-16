using System.Text.Json;
using TickAggregator.Domain.Entities;
using TickAggregator.Domain.Interfaces;

namespace TickAggregator.Infrastructure.Parsers;

public sealed class BinanceParser : IExchangeParser
{
    public string ExchangeName => "Binance";

    public IReadOnlyList<Tick> Parse(string rawMessage)
    {
        var doc = JsonDocument.Parse(rawMessage);
        var root = doc.RootElement;

        var tradeId = root.GetProperty("t").GetInt64().ToString();
        var symbol = root.GetProperty("s").GetString()!;
        var price = decimal.Parse(root.GetProperty("p").GetString()!,
            System.Globalization.CultureInfo.InvariantCulture);
        var quantity = decimal.Parse(root.GetProperty("q").GetString()!,
            System.Globalization.CultureInfo.InvariantCulture);
        var tradeTimeMs = root.GetProperty("T").GetInt64();
        var timestamp = DateTimeOffset.FromUnixTimeMilliseconds(tradeTimeMs);

        return [new Tick
        {
            TradeId = tradeId,
            Exchange = ExchangeName,
            Ticker = symbol,
            Price = price,
            Volume = quantity,
            Timestamp = timestamp,
            ReceivedAt = DateTimeOffset.UtcNow,
        }];
    }
}
