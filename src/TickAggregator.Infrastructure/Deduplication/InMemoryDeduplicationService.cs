using System.Collections.Concurrent;
using TickAggregator.Domain.Interfaces;

namespace TickAggregator.Infrastructure.Deduplication;

public sealed class InMemoryDeduplicationService : IDeduplicationService, IDisposable
{
    private readonly ConcurrentDictionary<string, DateTime> _seen = new();
    private readonly TimeSpan _ttl;
    private readonly Timer _cleanupTimer;

    public InMemoryDeduplicationService(TimeSpan? ttl = null)
    {
        _ttl = ttl ?? TimeSpan.FromMinutes(5);
        _cleanupTimer = new Timer(_ => Cleanup(), null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
    }

    public bool IsDuplicate(string exchange, string tradeId)
    {
        var key = $"{exchange}:{tradeId}";
        if (_seen.TryGetValue(key, out _))
            return true;

        _seen[key] = DateTime.UtcNow;
        return false;
    }

    private void Cleanup()
    {
        var cutoff = DateTime.UtcNow - _ttl;
        foreach (var kvp in _seen)
        {
            if (kvp.Value < cutoff)
                _seen.TryRemove(kvp.Key, out _);
        }
    }

    public void Dispose() => _cleanupTimer.Dispose();
}
