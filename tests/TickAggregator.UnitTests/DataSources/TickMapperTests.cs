using FluentAssertions;
using TickAggregator.Domain.Enums;
using TickAggregator.Infrastructure.DataSources.Binance;
using TickAggregator.Infrastructure.DataSources.Bybit;
using TickAggregator.Infrastructure.DataSources.Kraken;
using Xunit;

namespace TickAggregator.UnitTests.DataSources;

public sealed class KrakenTickMapperTests
{
    private readonly KrakenTickMapper _mapper = new();

    [Fact]
    public void Map_SetsAllFields()
    {
        var raw = new KrakenTick(
            TradeId: 789,
            Symbol: "BTC/USD",
            Price: 50000m,
            Qty: 0.25m,
            Timestamp: new DateTimeOffset(2024, 5, 18, 12, 0, 0, TimeSpan.Zero));

        var tick = _mapper.Map(raw);

        tick.TradeId.Should().Be("789");
        tick.Exchange.Should().Be(Exchange.Kraken);
        tick.Ticker.Should().Be("BTCUSD");
        tick.Price.Should().Be(50000m);
        tick.Volume.Should().Be(0.25m);
        tick.Timestamp.Should().Be(new DateTimeOffset(2024, 5, 18, 12, 0, 0, TimeSpan.Zero));
    }

    [Theory]
    [InlineData("BTC/USD", "BTCUSD")]
    [InlineData("ETH/EUR", "ETHEUR")]
    [InlineData("XRPUSDT", "XRPUSDT")]
    public void Map_RemovesSlashFromSymbol(string symbol, string expectedTicker)
    {
        var raw = new KrakenTick(1, symbol, 100m, 1m, DateTimeOffset.UtcNow);

        var tick = _mapper.Map(raw);

        tick.Ticker.Should().Be(expectedTicker);
    }
}

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
