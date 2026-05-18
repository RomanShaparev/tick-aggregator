using Microsoft.Extensions.Caching.Memory;

namespace TickAggregator.Infrastructure.Caching;

public sealed class InMemoryCache : ICache
{
    private readonly IMemoryCache _cache;

    public InMemoryCache(IMemoryCache cache) => _cache = cache;

    public bool Contains(string key) => _cache.TryGetValue(key, out _);

    public void Set(string key, TimeSpan ttl) => _cache.Set(key, true, ttl);
}
