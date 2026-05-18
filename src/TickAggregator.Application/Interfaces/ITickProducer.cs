using TickAggregator.Domain.Entities;

namespace TickAggregator.Application.Interfaces;

public interface ITickProducer
{
    Task PublishAsync(Tick tick, CancellationToken ct);
}
