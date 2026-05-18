using TickAggregator.Domain.Enums;

namespace TickAggregator.Domain.Entities;

public sealed class Tick
{
    public required string TradeId { get; init; }
    public Exchange Exchange { get; init; }
    public required string Ticker { get; init; }
    public decimal Price { get; init; }
    public decimal Volume { get; init; }
    public DateTimeOffset Timestamp { get; init; }
}
