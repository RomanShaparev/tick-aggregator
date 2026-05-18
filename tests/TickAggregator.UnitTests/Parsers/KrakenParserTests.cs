using FluentAssertions;
using TickAggregator.Domain.Enums;
using TickAggregator.Infrastructure.Parsers.Kraken;
using Xunit;

namespace TickAggregator.UnitTests.Parsers;

public sealed class KrakenParserTests
{
    private readonly KrakenParser _parser = new();

    [Fact]
    public void Parse_ValidMessage_ReturnsTick()
    {
        var json = """
            {
              "channel": "trade",
              "data": [{
                "trade_id": 59838473,
                "symbol": "BTC/USD",
                "side": "buy",
                "price": 50000.50,
                "qty": 0.0015,
                "timestamp": "2024-05-18T10:00:00.000000Z"
              }]
            }
            """;

        var ticks = _parser.Parse(json);

        ticks.Should().HaveCount(1);
        var tick = ticks[0];
        tick.TradeId.Should().Be("59838473");
        tick.Exchange.Should().Be(Exchange.Kraken);
        tick.Ticker.Should().Be("BTCUSD");
        tick.Price.Should().Be(50000.50m);
        tick.Volume.Should().Be(0.0015m);
    }

    [Fact]
    public void Parse_NoDataField_ReturnsEmpty()
    {
        var json = """{"channel":"heartbeat"}""";
        _parser.Parse(json).Should().BeEmpty();
    }

    [Fact]
    public void Parse_MultipleItems_ReturnsAll()
    {
        var json = """
            {
              "channel": "trade",
              "data": [
                {"trade_id":1,"symbol":"BTC/USD","side":"buy","price":50000.0,"qty":0.001,"timestamp":"2024-05-18T10:00:00.000000Z"},
                {"trade_id":2,"symbol":"ETH/USD","side":"sell","price":3000.0,"qty":0.1,"timestamp":"2024-05-18T10:00:01.000000Z"}
              ]
            }
            """;
        _parser.Parse(json).Should().HaveCount(2);
    }
}
