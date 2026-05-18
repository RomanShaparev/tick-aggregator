using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using TickAggregator.Domain.Enums;
using TickAggregator.Infrastructure.Caching;
using TickAggregator.Infrastructure.Configuration;
using TickAggregator.Infrastructure.Deduplication;
using Xunit;

namespace TickAggregator.UnitTests.Services;

public sealed class DeduplicationServiceTests
{
    private static DeduplicationService CreateService(double ttlSeconds = 300)
    {
        var cache = new InMemoryCache(new MemoryCache(new MemoryCacheOptions()));
        var options = Options.Create(new DeduplicationOptions { TtlSeconds = ttlSeconds });
        return new DeduplicationService(cache, options);
    }

    [Fact]
    public void FirstCall_IsNotDuplicate()
    {
        var svc = CreateService();
        svc.IsDuplicate(Exchange.Binance, "12345").Should().BeFalse();
    }

    [Fact]
    public void SecondCall_SameKey_IsDuplicate()
    {
        var svc = CreateService();
        svc.IsDuplicate(Exchange.Binance, "12345");
        svc.IsDuplicate(Exchange.Binance, "12345").Should().BeTrue();
    }

    [Fact]
    public void DifferentExchanges_SameTradeId_AreNotDuplicates()
    {
        var svc = CreateService();
        svc.IsDuplicate(Exchange.Binance, "12345").Should().BeFalse();
        svc.IsDuplicate(Exchange.Kraken, "12345").Should().BeFalse();
    }

    [Fact]
    public async Task AfterTtlExpiry_IsNotDuplicate()
    {
        var svc = CreateService(ttlSeconds: 0.05);
        svc.IsDuplicate(Exchange.Binance, "12345");
        await Task.Delay(100);
        svc.IsDuplicate(Exchange.Binance, "12345").Should().BeFalse();
    }
}
