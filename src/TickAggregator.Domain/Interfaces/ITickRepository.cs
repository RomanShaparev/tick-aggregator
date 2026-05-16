using TickAggregator.Domain.Entities;

namespace TickAggregator.Domain.Interfaces;

public interface ITickRepository
{
    Task InsertBatchAsync(IReadOnlyList<Tick> ticks, CancellationToken ct);
}
