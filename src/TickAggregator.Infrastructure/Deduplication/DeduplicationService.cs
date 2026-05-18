using Microsoft.Extensions.Options;
using TickAggregator.Domain.Enums;
using TickAggregator.Domain.Interfaces;
using TickAggregator.Infrastructure.Caching;
using TickAggregator.Infrastructure.Configuration;

namespace TickAggregator.Infrastructure.Deduplication;

public sealed class DeduplicationService : IDeduplicationService
{
    private readonly ICache _cache;
    private readonly TimeSpan _ttl;

    public DeduplicationService(ICache cache, IOptions<DeduplicationOptions> options)
    {
        _cache = cache;
        _ttl = TimeSpan.FromSeconds(options.Value.TtlSeconds);
    }

    public bool IsDuplicate(Exchange exchange, string tradeId)
    {
        var key = $"{exchange}:{tradeId}";
        if (_cache.Contains(key))
            return true;

        _cache.Set(key, _ttl);
        return false;
    }
}
