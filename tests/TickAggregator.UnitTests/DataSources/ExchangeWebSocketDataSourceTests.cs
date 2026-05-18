using System.Runtime.CompilerServices;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using TickAggregator.Domain.Entities;
using TickAggregator.Domain.Enums;
using TickAggregator.Infrastructure.DataSources;
using TickAggregator.Infrastructure.Messaging;
using TickAggregator.Infrastructure.WebSocket;
using Xunit;

namespace TickAggregator.UnitTests.DataSources;

public class FakeRawTick { }

public sealed class ExchangeWebSocketDataSourceTests
{

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

    private static Tick MakeTick(string tradeId) => new()
    {
        TradeId = tradeId,
        Exchange = Exchange.Binance,
        Ticker = "BTCUSDT",
        Price = 50000m,
        Volume = 1m,
        Timestamp = DateTimeOffset.UnixEpoch
    };

    private sealed class Fixture
    {
        public ITickParser<FakeRawTick> Parser { get; } = Substitute.For<ITickParser<FakeRawTick>>();
        public ITickMapper<FakeRawTick> Mapper { get; } = Substitute.For<ITickMapper<FakeRawTick>>();
        public IDlqProducer Dlq { get; } = Substitute.For<IDlqProducer>();
        public IExchangeWebSocketClient Client { get; } = Substitute.For<IExchangeWebSocketClient>();

        public ExchangeWebSocketDataSource<FakeRawTick> Build(
            IEnumerable<string> messages,
            Exchange exchange = Exchange.Binance)
        {
            Client.StreamAsync(Arg.Any<CancellationToken>()).Returns(Payloads(messages));
            return new ExchangeWebSocketDataSource<FakeRawTick>(
                Client, Parser, Mapper, Dlq, exchange, NullLogger.Instance);
        }
    }

    [Fact]
    public async Task ValidPayload_YieldsTick()
    {
        var f = new Fixture();
        var raw = new FakeRawTick();
        var expected = MakeTick("1");

        f.Parser.TryParse(Arg.Any<string>(), out Arg.Any<IEnumerable<FakeRawTick>>())
            .Returns(ci => { ci[1] = (IEnumerable<FakeRawTick>)[raw]; return true; });
        f.Mapper.Map(Arg.Any<FakeRawTick>()).Returns(expected);

        var ticks = await f.Build(["payload"]).StreamAsync(default).ToListAsync();

        ticks.Should().ContainSingle().Which.Should().Be(expected);
    }

    [Fact]
    public async Task InvalidPayload_YieldsNothing_AndSendsToDlq()
    {
        var f = new Fixture();
        f.Parser.TryParse(Arg.Any<string>(), out Arg.Any<IEnumerable<FakeRawTick>>()).Returns(false);

        var ticks = await f.Build(["bad-payload"], Exchange.Kraken).StreamAsync(default).ToListAsync();

        ticks.Should().BeEmpty();
        await f.Dlq.Received(1).SendAsync(
            Arg.Is<InvalidMessage>(m => m.Exchange == Exchange.Kraken && m.RawPayload == "bad-payload"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MultipleItemsFromParser_YieldsAll()
    {
        var f = new Fixture();
        var tick1 = MakeTick("1");
        var tick2 = MakeTick("2");

        f.Parser.TryParse(Arg.Any<string>(), out Arg.Any<IEnumerable<FakeRawTick>>())
            .Returns(ci => { ci[1] = (IEnumerable<FakeRawTick>)[new FakeRawTick(), new FakeRawTick()]; return true; });
        f.Mapper.Map(Arg.Any<FakeRawTick>()).Returns(tick1, tick2);

        var ticks = await f.Build(["payload"]).StreamAsync(default).ToListAsync();

        ticks.Should().Equal(tick1, tick2);
    }

    [Fact]
    public async Task MultipleMessages_YieldsFromAll()
    {
        var f = new Fixture();
        var tick1 = MakeTick("1");
        var tick2 = MakeTick("2");

        f.Parser.TryParse(Arg.Any<string>(), out Arg.Any<IEnumerable<FakeRawTick>>())
            .Returns(ci => { ci[1] = (IEnumerable<FakeRawTick>)[new FakeRawTick()]; return true; });
        f.Mapper.Map(Arg.Any<FakeRawTick>()).Returns(tick1, tick2);

        var ticks = await f.Build(["msg1", "msg2"]).StreamAsync(default).ToListAsync();

        ticks.Should().Equal(tick1, tick2);
    }

    [Fact]
    public async Task EmptyItemsFromParser_YieldsNothing()
    {
        var f = new Fixture();
        f.Parser.TryParse(Arg.Any<string>(), out Arg.Any<IEnumerable<FakeRawTick>>())
            .Returns(ci => { ci[1] = (IEnumerable<FakeRawTick>)[]; return true; });

        var ticks = await f.Build(["payload"]).StreamAsync(default).ToListAsync();

        ticks.Should().BeEmpty();
        f.Mapper.DidNotReceiveWithAnyArgs().Map(default!);
    }
}
