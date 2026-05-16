using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using TickAggregator.Application.Messages;
using TickAggregator.Application.Services;
using TickAggregator.Domain.Entities;
using TickAggregator.Domain.Interfaces;
using TickAggregator.Infrastructure.Deduplication;
using TickAggregator.Infrastructure.Parsers;
using Xunit;

namespace TickAggregator.UnitTests.Services;

public sealed class TickProcessingServiceTests
{
    private readonly ITickRepository _repository = Substitute.For<ITickRepository>();
    private readonly TickCounterService _counter = new();

    private TickProcessingService CreateService() =>
        new(
            [new BinanceParser(), new KrakenParser(), new BybitParser()],
            new InMemoryDeduplicationService(),
            _repository,
            _counter,
            NullLogger<TickProcessingService>.Instance);

    [Fact]
    public async Task ProcessBatch_ValidBinanceTick_CallsRepository()
    {
        var svc = CreateService();
        var message = new RawTickMessage
        {
            Exchange = "Binance",
            Payload = """{"e":"trade","t":1,"s":"BTCUSDT","p":"50000.00","q":"0.001","T":1716000000000}"""
        };

        var saved = await svc.ProcessBatchAsync([message], CancellationToken.None);

        saved.Should().Be(1);
        await _repository.Received(1).InsertBatchAsync(
            Arg.Is<IReadOnlyList<Tick>>(list => list.Count == 1 && list[0].TradeId == "1"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessBatch_DuplicateTick_SavedOnlyOnce()
    {
        var svc = CreateService();
        var message = new RawTickMessage
        {
            Exchange = "Binance",
            Payload = """{"e":"trade","t":42,"s":"BTCUSDT","p":"50000.00","q":"0.001","T":1716000000000}"""
        };

        var saved = await svc.ProcessBatchAsync([message, message], CancellationToken.None);

        saved.Should().Be(1);
        await _repository.Received(1).InsertBatchAsync(
            Arg.Is<IReadOnlyList<Tick>>(list => list.Count == 1),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessBatch_UnknownExchange_ReturnsZeroAndDoesNotThrow()
    {
        var svc = CreateService();
        var message = new RawTickMessage { Exchange = "UnknownExchange", Payload = "{}" };

        var saved = await svc.ProcessBatchAsync([message], CancellationToken.None);

        saved.Should().Be(0);
        await _repository.DidNotReceive().InsertBatchAsync(
            Arg.Any<IReadOnlyList<Tick>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessBatch_CounterIncrementedPerUniqueTick()
    {
        var svc = CreateService();
        var message = new RawTickMessage
        {
            Exchange = "Binance",
            Payload = """{"e":"trade","t":100,"s":"BTCUSDT","p":"50000.00","q":"0.001","T":1716000000000}"""
        };

        await svc.ProcessBatchAsync([message], CancellationToken.None);

        _counter.GetTotal().Should().Be(1);
        _counter.GetByExchange()["Binance"].Should().Be(1);
    }

    [Fact]
    public async Task ProcessBatch_EmptyBatch_DoesNotCallRepository()
    {
        var svc = CreateService();

        var saved = await svc.ProcessBatchAsync([], CancellationToken.None);

        saved.Should().Be(0);
        await _repository.DidNotReceive().InsertBatchAsync(
            Arg.Any<IReadOnlyList<Tick>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessBatch_RepositoryThrows_ExceptionPropagates()
    {
        _repository.InsertBatchAsync(Arg.Any<IReadOnlyList<Tick>>(), Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("DB error"));

        var svc = CreateService();
        var message = new RawTickMessage
        {
            Exchange = "Binance",
            Payload = """{"e":"trade","t":999,"s":"BTCUSDT","p":"50000.00","q":"0.001","T":1716000000000}"""
        };

        var act = async () => await svc.ProcessBatchAsync([message], CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
