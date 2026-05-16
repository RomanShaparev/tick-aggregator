using System.Collections.Concurrent;
using TickAggregator.Application.Interfaces;

namespace TickAggregator.Application.Services;

public sealed class TickCounterService : ITickCounter
{
    private long _total;
    private readonly ConcurrentDictionary<string, long> _byExchange = new();

    public void Increment(string exchange)
    {
        Interlocked.Increment(ref _total);
        _byExchange.AddOrUpdate(exchange, 1, (_, v) => v + 1);
    }

    public long GetTotal() => Interlocked.Read(ref _total);

    public IReadOnlyDictionary<string, long> GetByExchange() => _byExchange;
}
