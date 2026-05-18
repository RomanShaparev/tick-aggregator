using System.Runtime.CompilerServices;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using TickAggregator.Application.Interfaces;
using TickAggregator.Application.Services;
using TickAggregator.Domain.Entities;
using TickAggregator.Domain.Enums;
using TickAggregator.Domain.Interfaces;
using Xunit;

namespace TickAggregator.UnitTests.Services;

public sealed class TickCollectorServiceTests
{
    private static Tick MakeTick(string tradeId, Exchange exchange = Exchange.Binance) => new()
    {
        TradeId = tradeId,
        Exchange = exchange,
        Ticker = "BTCUSDT",
        Price = 50000m,
        Volume = 0.001m,
        Timestamp = DateTimeOffset.UtcNow,
    };

    private static async IAsyncEnumerable<Tick> TickStream(
        IEnumerable<Tick> ticks,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        foreach (var tick in ticks)
        {
            ct.ThrowIfCancellationRequested();
            yield return tick;
        }
    }

    [Fact]
    public async Task RunAsync_PublishesAllTicksFromSource()
    {
        var producer = Substitute.For<ITickProducer>();
        var source = Substitute.For<IExchangeDataSource>();
        source.StreamAsync(Arg.Any<CancellationToken>())
            .Returns(TickStream([MakeTick("1"), MakeTick("2"), MakeTick("3")]));

        var service = new TickCollectorService([source], producer, NullLogger<TickCollectorService>.Instance);
        await service.RunAsync(CancellationToken.None);

        await producer.Received(3).PublishAsync(Arg.Any<Tick>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunAsync_EmptySource_PublishesNothing()
    {
        var producer = Substitute.For<ITickProducer>();
        var source = Substitute.For<IExchangeDataSource>();
        source.StreamAsync(Arg.Any<CancellationToken>()).Returns(TickStream([]));

        var service = new TickCollectorService([source], producer, NullLogger<TickCollectorService>.Instance);
        await service.RunAsync(CancellationToken.None);

        await producer.DidNotReceive().PublishAsync(Arg.Any<Tick>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunAsync_MultipleSourcesConcurrently_AllPublished()
    {
        var producer = Substitute.For<ITickProducer>();

        var source1 = Substitute.For<IExchangeDataSource>();
        source1.StreamAsync(Arg.Any<CancellationToken>())
            .Returns(TickStream([MakeTick("a", Exchange.Binance), MakeTick("b", Exchange.Binance)]));

        var source2 = Substitute.For<IExchangeDataSource>();
        source2.StreamAsync(Arg.Any<CancellationToken>())
            .Returns(TickStream([MakeTick("c", Exchange.Kraken)]));

        var service = new TickCollectorService(
            [source1, source2], producer, NullLogger<TickCollectorService>.Instance);
        await service.RunAsync(CancellationToken.None);

        await producer.Received(3).PublishAsync(Arg.Any<Tick>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunAsync_CancellationRequested_StopsGracefully()
    {
        var producer = Substitute.For<ITickProducer>();
        using var cts = new CancellationTokenSource();

        var source = Substitute.For<IExchangeDataSource>();
        source.StreamAsync(Arg.Any<CancellationToken>())
            .Returns(InfiniteStream(cts.Token));

        cts.Cancel();
        var service = new TickCollectorService([source], producer, NullLogger<TickCollectorService>.Instance);

        var act = async () => await service.RunAsync(cts.Token);
        await act.Should().NotThrowAsync();
    }

    private static async IAsyncEnumerable<Tick> InfiniteStream(
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        while (true)
        {
            ct.ThrowIfCancellationRequested();
            yield return new Tick
            {
                TradeId = Guid.NewGuid().ToString(),
                Exchange = Exchange.Binance,
                Ticker = "BTCUSDT",
                Price = 50000m,
                Volume = 0.001m,
                Timestamp = DateTimeOffset.UtcNow,
            };
            await Task.Yield();
        }
    }
}
