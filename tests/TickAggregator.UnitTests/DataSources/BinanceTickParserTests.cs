using FluentAssertions;
using TickAggregator.Infrastructure.DataSources.Binance;
using Xunit;

namespace TickAggregator.UnitTests.DataSources;

public sealed class BinanceTickParserTests
{
    private readonly BinanceTickParser _parser = new();

    [Fact]
    public void TryParse_ValidPayload_ReturnsTrueWithItem()
    {
        const string json = """{"t":123,"s":"BTCUSDT","p":"50000.00","q":"0.5","T":1716000000000}""";

        var result = _parser.TryParse(json, out var items);

        result.Should().BeTrue();
        items.Should().HaveCount(1);
        items.Single().TradeId.Should().Be(123);
        items.Single().Symbol.Should().Be("BTCUSDT");
    }

    [Fact]
    public void TryParse_InvalidJson_ReturnsFalse()
    {
        var result = _parser.TryParse("not-json", out var items);

        result.Should().BeFalse();
        items.Should().BeEmpty();
    }

    [Fact]
    public void TryParse_EmptyJson_ReturnsFalse()
    {
        var result = _parser.TryParse("{}", out _);

        result.Should().BeTrue();
    }
}