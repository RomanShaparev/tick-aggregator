using FluentAssertions;
using TickAggregator.Domain.Enums;
using TickAggregator.Infrastructure.DataSources.Binance;
using Xunit;

namespace TickAggregator.UnitTests.DataSources;

public sealed class BinanceTickMapperTests
{
    private readonly BinanceTickMapper _mapper = new();

    [Fact]
    public void Map_SetsAllFields()
    {
        var raw = new BinanceTick(
            TradeId: 123,
            Symbol: "BTCUSDT",
            Price: "50000.00",
            Quantity: "0.5",
            TradeTimeMs: 1716000000000);

        var tick = _mapper.Map(raw);

        tick.TradeId.Should().Be("123");
        tick.Exchange.Should().Be(Exchange.Binance);
        tick.Ticker.Should().Be("BTCUSDT");
        tick.Price.Should().Be(50000.00m);
        tick.Volume.Should().Be(0.5m);
        tick.Timestamp.Should().Be(DateTimeOffset.FromUnixTimeMilliseconds(1716000000000));
    }

    [Fact]
    public void Map_ParsesDecimalsWithInvariantCulture()
    {
        var raw = new BinanceTick(1, "ETHUSDT", "3000.123456789", "0.000001", 1716000000000);

        var tick = _mapper.Map(raw);

        tick.Price.Should().Be(3000.123456789m);
        tick.Volume.Should().Be(0.000001m);
    }
}