using TickAggregator.Domain.Entities;

namespace TickAggregator.Infrastructure.Messaging;

public interface IMessageProducer
{
    Task PublishAsync(Tick tick, CancellationToken ct);
}
