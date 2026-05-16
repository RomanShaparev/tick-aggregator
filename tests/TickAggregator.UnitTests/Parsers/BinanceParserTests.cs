using FluentAssertions;
using TickAggregator.Infrastructure.Parsers;
using Xunit;

namespace TickAggregator.UnitTests.Parsers;

public sealed class BinanceParserTests
{
    private readonly BinanceParser _parser = new();

    [Fact]
    public void Parse_ValidMessage_ReturnsTick()
    {
        var json = """{"e":"trade","t":12345,"s":"BTCUSDT","p":"50000.50","q":"0.001500","T":1716000000000}""";

        var ticks = _parser.Parse(json);

        ticks.Should().HaveCount(1);
        var tick = ticks[0];
        tick.TradeId.Should().Be("12345");
        tick.Exchange.Should().Be("Binance");
        tick.Ticker.Should().Be("BTCUSDT");
        tick.Price.Should().Be(50000.50m);
        tick.Volume.Should().Be(0.001500m);
        tick.Timestamp.Should().Be(DateTimeOffset.FromUnixTimeMilliseconds(1716000000000));
    }

    [Fact]
    public void Parse_InvalidJson_Throws()
    {
        var act = () => _parser.Parse("not-json");
        act.Should().Throw<Exception>();
    }

    [Fact]
    public void ExchangeName_IsBinance()
    {
        _parser.ExchangeName.Should().Be("Binance");
    }
}
