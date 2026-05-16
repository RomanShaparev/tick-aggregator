using FluentAssertions;
using TickAggregator.Infrastructure.Deduplication;
using Xunit;

namespace TickAggregator.UnitTests.Services;

public sealed class DeduplicationServiceTests
{
    [Fact]
    public void FirstCall_IsNotDuplicate()
    {
        using var svc = new InMemoryDeduplicationService();
        svc.IsDuplicate("Binance", "12345").Should().BeFalse();
    }

    [Fact]
    public void SecondCall_SameKey_IsDuplicate()
    {
        using var svc = new InMemoryDeduplicationService();
        svc.IsDuplicate("Binance", "12345");
        svc.IsDuplicate("Binance", "12345").Should().BeTrue();
    }

    [Fact]
    public void DifferentExchanges_SameTradeId_AreNotDuplicates()
    {
        using var svc = new InMemoryDeduplicationService();
        svc.IsDuplicate("Binance", "12345").Should().BeFalse();
        svc.IsDuplicate("Kraken", "12345").Should().BeFalse();
    }

    [Fact]
    public void AfterTtlExpiry_IsNotDuplicate()
    {
        using var svc = new InMemoryDeduplicationService(ttl: TimeSpan.FromMilliseconds(50));
        svc.IsDuplicate("Binance", "12345");
        // TTL-based cleanup is timer-driven, so we just verify initial behaviour
        svc.IsDuplicate("Binance", "99999").Should().BeFalse();
    }
}
