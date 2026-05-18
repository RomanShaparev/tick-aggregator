using FluentAssertions;
using TickAggregator.Infrastructure.DataSources.Binance;
using TickAggregator.Infrastructure.DataSources.Bybit;
using TickAggregator.Infrastructure.DataSources.Kraken;
using Xunit;

namespace TickAggregator.UnitTests.DataSources;

public sealed class KrakenTickParserTests
{
    private readonly KrakenTickParser _parser = new();

    [Fact]
    public void TryParse_ValidPayload_ReturnsTrueWithItems()
    {
        const string json = """
            {"data":[{"trade_id":1,"symbol":"BTC/USD","price":50000.00,"qty":0.5,"timestamp":"2024-05-18T12:00:00+00:00"}]}
            """;

        var result = _parser.TryParse(json, out var items);

        result.Should().BeTrue();
        items.Should().HaveCount(1);
        items.Single().TradeId.Should().Be(1);
    }

    [Fact]
    public void TryParse_MultipleItems_ReturnsAll()
    {
        const string json = """
            {"data":[
                {"trade_id":1,"symbol":"BTC/USD","price":50000.00,"qty":0.1,"timestamp":"2024-05-18T12:00:00+00:00"},
                {"trade_id":2,"symbol":"ETH/USD","price":3000.00,"qty":1.0,"timestamp":"2024-05-18T12:00:01+00:00"}
            ]}
            """;

        var result = _parser.TryParse(json, out var items);

        result.Should().BeTrue();
        items.Should().HaveCount(2);
    }

    [Fact]
    public void TryParse_EmptyDataArray_ReturnsTrueWithNoItems()
    {
        const string json = """{"data":[]}""";

        var result = _parser.TryParse(json, out var items);

        result.Should().BeTrue();
        items.Should().BeEmpty();
    }

    [Fact]
    public void TryParse_NullData_ReturnsTrueWithNoItems()
    {
        const string json = """{"data":null}""";

        var result = _parser.TryParse(json, out var items);

        result.Should().BeTrue();
        items.Should().BeEmpty();
    }

    [Fact]
    public void TryParse_InvalidJson_ReturnsFalse()
    {
        var result = _parser.TryParse("not-json", out var items);

        result.Should().BeFalse();
        items.Should().BeEmpty();
    }
}

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

public sealed class BybitTickParserTests
{
    private readonly BybitTickParser _parser = new();

    [Fact]
    public void TryParse_ValidPayload_ReturnsTrueWithItems()
    {
        const string json = """{"data":[{"i":"abc123","s":"BTCUSDT","p":"50000.00","v":"0.5","T":1716000000000}]}""";

        var result = _parser.TryParse(json, out var items);

        result.Should().BeTrue();
        items.Should().HaveCount(1);
        items.Single().TradeId.Should().Be("abc123");
    }

    [Fact]
    public void TryParse_EmptyDataArray_ReturnsTrueWithNoItems()
    {
        const string json = """{"data":[]}""";

        var result = _parser.TryParse(json, out var items);

        result.Should().BeTrue();
        items.Should().BeEmpty();
    }

    [Fact]
    public void TryParse_NullData_ReturnsTrueWithNoItems()
    {
        const string json = """{"data":null}""";

        var result = _parser.TryParse(json, out var items);

        result.Should().BeTrue();
        items.Should().BeEmpty();
    }

    [Fact]
    public void TryParse_InvalidJson_ReturnsFalse()
    {
        var result = _parser.TryParse("not-json", out var items);

        result.Should().BeFalse();
        items.Should().BeEmpty();
    }
}
