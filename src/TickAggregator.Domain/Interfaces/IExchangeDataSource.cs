using TickAggregator.Domain.Entities;

namespace TickAggregator.Domain.Interfaces;

public interface IExchangeDataSource
{
    IAsyncEnumerable<Tick> StreamAsync(CancellationToken ct);
}