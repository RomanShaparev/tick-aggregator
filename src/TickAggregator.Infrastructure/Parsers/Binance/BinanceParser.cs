using System.Text.Json;
using TickAggregator.Domain.Entities;
using TickAggregator.Domain.Enums;
using TickAggregator.Infrastructure.Parsers;

namespace TickAggregator.Infrastructure.Parsers.Binance;

public sealed class BinanceParser : IExchangeParser
{
    public Exchange Exchange => Exchange.Binance;

    public IReadOnlyList<Tick> Parse(string rawMessage)
    {
        var raw = JsonSerializer.Deserialize<BinanceTick>(rawMessage)!;
        return [raw.ToTick(Exchange)];
    }
}
