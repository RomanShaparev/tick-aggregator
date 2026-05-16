using TickAggregator.Application.Messages;

namespace TickAggregator.Application.Interfaces;

public interface IMessageProducer
{
    Task PublishAsync(RawTickMessage message, CancellationToken ct);
}
