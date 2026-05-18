using System.Runtime.CompilerServices;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using TickAggregator.Domain.Enums;
using TickAggregator.Infrastructure.DataSources.Binance;
using TickAggregator.Infrastructure.WebSocket;
using Xunit;

namespace TickAggregator.UnitTests.DataSources;

public sealed class BinanceWebSocketDataSourceTests
{
    private static BinanceWebSocketDataSource CreateSource(IExchangeWebSocketClient client)
        => new(client, new BinanceParser(), new BinanceMapper(), NullLogger<BinanceWebSocketDataSource>.Instance);

    private static async IAsyncEnumerable<string> Payloads(
        IEnumerable<string> items,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        foreach (var item in items)
        {
            ct.ThrowIfCancellationRequested();
            yield return item;
        }
    }

    [Fact]
    public async Task ValidPayload_YieldsTick()
    {
        const string json = """{"t":123,"s":"BTCUSDT","p":"50000.00","q":"0.5","T":1716000000000}""";

        var client = Substitute.For<IExchangeWebSocketClient>();
        client.StreamAsync(Arg.Any<CancellationToken>()).Returns(Payloads([json]));

        var ticks = await CreateSource(client).StreamAsync(default).ToListAsync();

        ticks.Should().HaveCount(1);
        var tick = ticks[0];
        tick.TradeId.Should().Be("123");
        tick.Exchange.Should().Be(Exchange.Binance);
        tick.Ticker.Should().Be("BTCUSDT");
        tick.Price.Should().Be(50000.00m);
        tick.Volume.Should().Be(0.5m);
        tick.Timestamp.Should().Be(DateTimeOffset.FromUnixTimeMilliseconds(1716000000000));
    }

    [Fact]
    public async Task InvalidJson_YieldsNothing()
    {
        var client = Substitute.For<IExchangeWebSocketClient>();
        client.StreamAsync(Arg.Any<CancellationToken>()).Returns(Payloads(["not-json"]));

        var ticks = await CreateSource(client).StreamAsync(default).ToListAsync();

        ticks.Should().BeEmpty();
    }

    [Fact]
    public async Task MultiplePayloads_YieldsMultipleTicks()
    {
        const string json1 = """{"t":1,"s":"BTCUSDT","p":"50000.00","q":"0.1","T":1716000000001}""";
        const string json2 = """{"t":2,"s":"ETHUSDT","p":"3000.00","q":"1.0","T":1716000000002}""";

        var client = Substitute.For<IExchangeWebSocketClient>();
        client.StreamAsync(Arg.Any<CancellationToken>()).Returns(Payloads([json1, json2]));

        var ticks = await CreateSource(client).StreamAsync(default).ToListAsync();

        ticks.Should().HaveCount(2);
        ticks[0].TradeId.Should().Be("1");
        ticks[1].TradeId.Should().Be("2");
    }
}
