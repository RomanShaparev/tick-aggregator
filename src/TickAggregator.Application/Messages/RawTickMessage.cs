namespace TickAggregator.Application.Messages;

public sealed record RawTickMessage
{
    public required string Exchange { get; init; }
    public required string Payload { get; init; }
}
