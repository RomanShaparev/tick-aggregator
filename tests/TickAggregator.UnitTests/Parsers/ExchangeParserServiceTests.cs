using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using TickAggregator.Domain.Enums;
using TickAggregator.Infrastructure.Parsers;
using TickAggregator.Infrastructure.Parsers.Binance;
using TickAggregator.Infrastructure.Parsers.Bybit;
using TickAggregator.Infrastructure.Parsers.Kraken;
using Xunit;

namespace TickAggregator.UnitTests.Parsers;

public sealed class ExchangeParserServiceTests
{
    private static readonly ExchangeParserService AllParsers = new(
        [new BinanceParser(), new BybitParser(), new KrakenParser()],
        NullLogger<ExchangeParserService>.Instance);

    [Fact]
    public void Parse_ValidBinanceMessage_ReturnsTick()
    {
        var ticks = AllParsers.Parse(Exchange.Binance,
            """{"e":"trade","t":1,"s":"BTCUSDT","p":"50000.00","q":"0.001","T":1716000000000}""");

        ticks.Should().HaveCount(1);
        ticks[0].Exchange.Should().Be(Exchange.Binance);
    }

    [Fact]
    public void Parse_NoParserRegistered_ReturnsEmpty()
    {
        var service = new ExchangeParserService(
            [new BinanceParser()],
            NullLogger<ExchangeParserService>.Instance);

        service.Parse(Exchange.Kraken, "{}").Should().BeEmpty();
    }

    [Fact]
    public void Parse_InvalidPayload_ReturnsEmpty()
    {
        AllParsers.Parse(Exchange.Binance, "not-json").Should().BeEmpty();
    }
}
