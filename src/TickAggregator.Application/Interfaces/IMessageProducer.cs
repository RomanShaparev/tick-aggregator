using TickAggregator.Domain.Entities;

namespace TickAggregator.Application.Interfaces;

public interface IMessageProducer
{
    Task PublishAsync(Tick tick, CancellationToken ct);
}
