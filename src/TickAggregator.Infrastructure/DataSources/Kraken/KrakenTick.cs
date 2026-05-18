using System.Text.Json.Serialization;

namespace TickAggregator.Infrastructure.DataSources.Kraken;

internal sealed record KrakenMessage(
    [property: JsonPropertyName("data")] KrakenTick[]? Data
);

internal sealed record KrakenTick(
    [property: JsonPropertyName("trade_id")] long TradeId,
    [property: JsonPropertyName("symbol")] string Symbol,
    [property: JsonPropertyName("price")] decimal Price,
    [property: JsonPropertyName("qty")] decimal Qty,
    [property: JsonPropertyName("timestamp")] DateTimeOffset Timestamp
);
