using TickAggregator.Domain.Enums;

namespace TickAggregator.Domain.Entities;

public sealed class Tick
{
    public string TradeId { get; init; } = default!;
    public Exchange Exchange { get; init; }
    public string Ticker { get; init; } = default!;
    public decimal Price { get; init; }
    public decimal Volume { get; init; }
    public DateTimeOffset Timestamp { get; init; }
    public DateTimeOffset ReceivedAt { get; init; }
}
