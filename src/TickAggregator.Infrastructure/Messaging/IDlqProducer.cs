namespace TickAggregator.Infrastructure.Messaging;

public interface IDlqProducer
{
    Task SendAsync(InvalidMessage message, CancellationToken ct);
}
