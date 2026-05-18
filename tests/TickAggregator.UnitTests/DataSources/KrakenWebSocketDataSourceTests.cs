using System.Runtime.CompilerServices;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using TickAggregator.Domain.Enums;
using TickAggregator.Infrastructure.DataSources;
using TickAggregator.Infrastructure.DataSources.Kraken;
using TickAggregator.Infrastructure.Messaging;
using TickAggregator.Infrastructure.WebSocket;
using Xunit;

namespace TickAggregator.UnitTests.DataSources;

public sealed class KrakenWebSocketDataSourceTests
{
    private static ExchangeWebSocketDataSource<KrakenTick> CreateSource(IExchangeWebSocketClient client)
        => new(client, new KrakenTickParser(), new KrakenTickMapper(), Substitute.For<IDlqProducer>(), Exchange.Kraken, NullLogger.Instance);

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
        const string json = """
            {"data":[{"trade_id":456,"symbol":"BTC/USD","price":50000.00,"qty":0.5,"timestamp":"2024-05-18T12:00:00+00:00"}]}
            """;

        var client = Substitute.For<IExchangeWebSocketClient>();
        client.StreamAsync(Arg.Any<CancellationToken>()).Returns(Payloads([json]));

        var ticks = await CreateSource(client).StreamAsync(default).ToListAsync();

        ticks.Should().HaveCount(1);
        var tick = ticks[0];
        tick.TradeId.Should().Be("456");
        tick.Exchange.Should().Be(Exchange.Kraken);
        tick.Ticker.Should().Be("BTCUSD");
        tick.Price.Should().Be(50000.00m);
        tick.Volume.Should().Be(0.5m);
        tick.Timestamp.Should().Be(new DateTimeOffset(2024, 5, 18, 12, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    public async Task SymbolSlashIsRemoved()
    {
        const string json = """
            {"data":[{"trade_id":1,"symbol":"ETH/USD","price":3000.00,"qty":1.0,"timestamp":"2024-05-18T12:00:00+00:00"}]}
            """;

        var client = Substitute.For<IExchangeWebSocketClient>();
        client.StreamAsync(Arg.Any<CancellationToken>()).Returns(Payloads([json]));

        var ticks = await CreateSource(client).StreamAsync(default).ToListAsync();

        ticks.Single().Ticker.Should().Be("ETHUSD");
    }

    [Fact]
    public async Task EmptyDataArray_YieldsNothing()
    {
        const string json = """{"data":[]}""";

        var client = Substitute.For<IExchangeWebSocketClient>();
        client.StreamAsync(Arg.Any<CancellationToken>()).Returns(Payloads([json]));

        var ticks = await CreateSource(client).StreamAsync(default).ToListAsync();

        ticks.Should().BeEmpty();
    }

    [Fact]
    public async Task NullData_YieldsNothing()
    {
        const string json = """{"data":null}""";

        var client = Substitute.For<IExchangeWebSocketClient>();
        client.StreamAsync(Arg.Any<CancellationToken>()).Returns(Payloads([json]));

        var ticks = await CreateSource(client).StreamAsync(default).ToListAsync();

        ticks.Should().BeEmpty();
    }

    [Fact]
    public async Task MultipleTicksInMessage_YieldsAll()
    {
        const string json = """
            {"data":[
                {"trade_id":1,"symbol":"BTC/USD","price":50000.00,"qty":0.1,"timestamp":"2024-05-18T12:00:00+00:00"},
                {"trade_id":2,"symbol":"ETH/USD","price":3000.00,"qty":1.0,"timestamp":"2024-05-18T12:00:01+00:00"}
            ]}
            """;

        var client = Substitute.For<IExchangeWebSocketClient>();
        client.StreamAsync(Arg.Any<CancellationToken>()).Returns(Payloads([json]));

        var ticks = await CreateSource(client).StreamAsync(default).ToListAsync();

        ticks.Should().HaveCount(2);
        ticks[0].TradeId.Should().Be("1");
        ticks[1].TradeId.Should().Be("2");
    }

    [Fact]
    public async Task InvalidJson_YieldsNothing_AndSendsToDlq()
    {
        var dlq = Substitute.For<IDlqProducer>();
        var client = Substitute.For<IExchangeWebSocketClient>();
        client.StreamAsync(Arg.Any<CancellationToken>()).Returns(Payloads(["not-json"]));
        var source = new ExchangeWebSocketDataSource<KrakenTick>(
            client, new KrakenTickParser(), new KrakenTickMapper(), dlq, Exchange.Kraken, NullLogger.Instance);

        var ticks = await source.StreamAsync(default).ToListAsync();

        ticks.Should().BeEmpty();
        await dlq.Received(1).SendAsync(
            Arg.Is<InvalidMessage>(m => m.Exchange == Exchange.Kraken && m.RawPayload == "not-json"),
            Arg.Any<CancellationToken>());
    }
}
