using System.Text.Json.Serialization;

namespace TickAggregator.Infrastructure.DataSources.Bybit;

public sealed record BybitMessage(
    [property: JsonPropertyName("data")] BybitTick[]? Data
);

public sealed record BybitTick(
    [property: JsonPropertyName("i")] string TradeId,
    [property: JsonPropertyName("s")] string Symbol,
    [property: JsonPropertyName("p")] string Price,
    [property: JsonPropertyName("v")] string Volume,
    [property: JsonPropertyName("T")] long TradeTimeMs
);
