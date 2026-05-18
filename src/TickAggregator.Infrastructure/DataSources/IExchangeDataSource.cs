using TickAggregator.Domain.Enums;

namespace TickAggregator.Infrastructure.DataSources;

public interface IExchangeDataSource
{
    Exchange Exchange { get; }
    IAsyncEnumerable<string> StreamAsync(CancellationToken ct);
}
