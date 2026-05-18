using System.Text.Json.Serialization;

namespace TickAggregator.Infrastructure.DataSources.Binance;

public sealed record BinanceTick(
    [property: JsonPropertyName("t")] long TradeId,
    [property: JsonPropertyName("s")] string Symbol,
    [property: JsonPropertyName("p")] string Price,
    [property: JsonPropertyName("q")] string Quantity,
    [property: JsonPropertyName("T")] long TradeTimeMs
);
