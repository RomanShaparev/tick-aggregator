using System.Runtime.CompilerServices;
using TickAggregator.Domain.Entities;
using TickAggregator.Domain.Interfaces;

namespace TickAggregator.EndToEndTests.Infrastructure;

public sealed class FakeExchangeDataSource(IReadOnlyList<Tick> ticks) : IExchangeDataSource
{
    public async IAsyncEnumerable<Tick> StreamAsync([EnumeratorCancellation] CancellationToken ct)
    {
        foreach (var tick in ticks)
        {
            ct.ThrowIfCancellationRequested();
            yield return tick;
        }
        await Task.Delay(Timeout.Infinite, ct);
    }
}
