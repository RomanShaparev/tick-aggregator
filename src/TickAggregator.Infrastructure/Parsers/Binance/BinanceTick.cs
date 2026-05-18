using System.Text.Json.Serialization;

namespace TickAggregator.Infrastructure.Parsers.Binance;

internal sealed record BinanceTick(
    [property: JsonPropertyName("t")] long TradeId,
    [property: JsonPropertyName("s")] string Symbol,
    [property: JsonPropertyName("p")] string Price,
    [property: JsonPropertyName("q")] string Quantity,
    [property: JsonPropertyName("T")] long TradeTimeMs
);
