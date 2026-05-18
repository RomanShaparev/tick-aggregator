using FluentAssertions;
using TickAggregator.Domain.Enums;
using TickAggregator.Infrastructure.Parsers.Bybit;
using Xunit;

namespace TickAggregator.UnitTests.Parsers;

public sealed class BybitParserTests
{
    private readonly BybitParser _parser = new();

    [Fact]
    public void Parse_ValidMessage_ReturnsTick()
    {
        var json = """
            {
              "topic": "publicTrade.BTCUSDT",
              "ts": 1716000000000,
              "data": [{
                "i": "abc123xyz456def7",
                "T": 1716000000000,
                "p": "50000.50",
                "v": "0.001500",
                "S": "Buy",
                "s": "BTCUSDT"
              }]
            }
            """;

        var ticks = _parser.Parse(json);

        ticks.Should().HaveCount(1);
        var tick = ticks[0];
        tick.TradeId.Should().Be("abc123xyz456def7");
        tick.Exchange.Should().Be(Exchange.Bybit);
        tick.Ticker.Should().Be("BTCUSDT");
        tick.Price.Should().Be(50000.50m);
        tick.Volume.Should().Be(0.001500m);
        tick.Timestamp.Should().Be(DateTimeOffset.FromUnixTimeMilliseconds(1716000000000));
    }

    [Fact]
    public void Parse_NoDataField_ReturnsEmpty()
    {
        var json = """{"topic":"heartbeat"}""";
        _parser.Parse(json).Should().BeEmpty();
    }

    [Fact]
    public void Exchange_IsBybit()
    {
        _parser.Exchange.Should().Be(Exchange.Bybit);
    }
}
