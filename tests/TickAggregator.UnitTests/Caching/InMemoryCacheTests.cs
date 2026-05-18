using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using TickAggregator.Infrastructure.Caching;
using Xunit;

namespace TickAggregator.UnitTests.Caching;

public sealed class InMemoryCacheTests
{
    private static InMemoryCache CreateCache() =>
        new(new MemoryCache(new MemoryCacheOptions()));

    [Fact]
    public void Contains_KeyNotSet_ReturnsFalse()
    {
        var cache = CreateCache();

        cache.Contains("missing").Should().BeFalse();
    }

    [Fact]
    public void Contains_AfterSet_ReturnsTrue()
    {
        var cache = CreateCache();

        cache.Set("key1", TimeSpan.FromMinutes(1));

        cache.Contains("key1").Should().BeTrue();
    }

    [Fact]
    public void Contains_DifferentKey_ReturnsFalse()
    {
        var cache = CreateCache();
        cache.Set("key1", TimeSpan.FromMinutes(1));

        cache.Contains("key2").Should().BeFalse();
    }

    [Fact]
    public async Task Contains_AfterTtlExpiry_ReturnsFalse()
    {
        var cache = CreateCache();
        cache.Set("short", TimeSpan.FromMilliseconds(50));

        await Task.Delay(150);

        cache.Contains("short").Should().BeFalse();
    }

    [Fact]
    public void Set_MultipleKeys_EachTrackedIndependently()
    {
        var cache = CreateCache();

        cache.Set("a", TimeSpan.FromMinutes(1));
        cache.Set("b", TimeSpan.FromMinutes(1));

        cache.Contains("a").Should().BeTrue();
        cache.Contains("b").Should().BeTrue();
        cache.Contains("c").Should().BeFalse();
    }
}
