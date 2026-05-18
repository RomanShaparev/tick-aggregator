namespace TickAggregator.Infrastructure.Caching;

public interface ICache
{
    bool Contains(string key);
    void Set(string key, TimeSpan ttl);
}
