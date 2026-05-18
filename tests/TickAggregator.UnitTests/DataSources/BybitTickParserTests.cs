using FluentAssertions;
using TickAggregator.Infrastructure.DataSources.Bybit;
using Xunit;

namespace TickAggregator.UnitTests.DataSources;

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