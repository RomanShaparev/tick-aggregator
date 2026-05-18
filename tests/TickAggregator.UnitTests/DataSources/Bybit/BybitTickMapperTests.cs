using FluentAssertions;
using TickAggregator.Domain.Enums;
using TickAggregator.Infrastructure.DataSources.Bybit;
using Xunit;

namespace TickAggregator.UnitTests.DataSources.Bybit;

public sealed class BybitTickMapperTests
{
    private readonly BybitTickMapper _mapper = new();

    [Fact]
    public void Map_SetsAllFields()
    {
        var raw = new BybitTick(
            TradeId: "abc123",
            Symbol: "BTCUSDT",
            Price: "50000.00",
            Volume: "0.5",
            TradeTimeMs: 1716000000000);

        var tick = _mapper.Map(raw);

        tick.TradeId.Should().Be("abc123");
        tick.Exchange.Should().Be(Exchange.Bybit);
        tick.Ticker.Should().Be("BTCUSDT");
        tick.Price.Should().Be(50000.00m);
        tick.Volume.Should().Be(0.5m);
        tick.Timestamp.Should().Be(DateTimeOffset.FromUnixTimeMilliseconds(1716000000000));
    }

    [Fact]
    public void Map_ParsesDecimalsWithInvariantCulture()
    {
        var raw = new BybitTick("id1", "SOLUSDT", "150.99", "10.123", 1716000000000);

        var tick = _mapper.Map(raw);

        tick.Price.Should().Be(150.99m);
        tick.Volume.Should().Be(10.123m);
    }
}
