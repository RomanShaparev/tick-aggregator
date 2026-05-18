using FluentAssertions;
using TickAggregator.Domain.Enums;
using TickAggregator.Infrastructure.DataSources.Kraken;
using Xunit;

namespace TickAggregator.UnitTests.DataSources.Kraken;

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