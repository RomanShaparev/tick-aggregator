using System.Diagnostics.Metrics;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using TickAggregator.Application.Metrics;
using TickAggregator.Application.Services;
using TickAggregator.Domain.Entities;
using TickAggregator.Domain.Enums;
using TickAggregator.Domain.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using TickAggregator.Infrastructure.Caching;
using TickAggregator.Infrastructure.Configuration;
using TickAggregator.Infrastructure.Deduplication;
using Xunit;

namespace TickAggregator.UnitTests.Services;

public sealed class TickProcessingServiceTests : IDisposable
{
    private readonly ITickRepository _repository = Substitute.For<ITickRepository>();
    private readonly TickMetrics _metrics = new();

    public void Dispose() => _metrics.Dispose();

    private TickProcessingService CreateService() =>
        new(new DeduplicationService(
                new InMemoryCache(new MemoryCache(new MemoryCacheOptions())),
                Options.Create(new DeduplicationOptions { TtlSeconds = 300 })),
            _repository,
            _metrics,
            NullLogger<TickProcessingService>.Instance);

    private static Tick MakeTick(string tradeId = "1", Exchange exchange = Exchange.Binance) => new()
    {
        TradeId = tradeId,
        Exchange = exchange,
        Ticker = "BTCUSDT",
        Price = 50000m,
        Volume = 0.001m,
        Timestamp = DateTimeOffset.UtcNow,
    };

    [Fact]
    public async Task ProcessBatch_ValidTick_CallsRepository()
    {
        await CreateService().ProcessBatchAsync([MakeTick()], CancellationToken.None);

        await _repository.Received(1).InsertBatchAsync(
            Arg.Is<IReadOnlyList<Tick>>(list => list.Count == 1 && list[0].TradeId == "1"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessBatch_DuplicateTick_SavedOnlyOnce()
    {
        var tick = MakeTick("42");

        await CreateService().ProcessBatchAsync([tick, tick], CancellationToken.None);

        await _repository.Received(1).InsertBatchAsync(
            Arg.Is<IReadOnlyList<Tick>>(list => list.Count == 1),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessBatch_EmptyBatch_DoesNotCallRepository()
    {
        await CreateService().ProcessBatchAsync([], CancellationToken.None);

        await _repository.DidNotReceive().InsertBatchAsync(
            Arg.Any<IReadOnlyList<Tick>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessBatch_CounterIncrementedPerUniqueTick()
    {
        long captured = 0;
        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, l) =>
        {
            if (instrument.Meter.Name == TickMetrics.MeterName)
                l.EnableMeasurementEvents(instrument);
        };
        listener.SetMeasurementEventCallback<long>((_, value, _, _) =>
            Interlocked.Add(ref captured, value));
        listener.Start();

        await CreateService().ProcessBatchAsync([MakeTick()], CancellationToken.None);

        captured.Should().Be(1);
    }

    [Fact]
    public async Task ProcessBatch_MetricsTaggedWithExchange()
    {
        var captured = new List<(long Value, string? Exchange)>();
        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, l) =>
        {
            if (instrument.Meter.Name == TickMetrics.MeterName)
                l.EnableMeasurementEvents(instrument);
        };
        listener.SetMeasurementEventCallback<long>((_, value, tags, _) =>
        {
            var exchange = tags.ToArray()
                .FirstOrDefault(t => t.Key == "exchange")
                .Value?.ToString();
            captured.Add((value, exchange));
        });
        listener.Start();

        await CreateService().ProcessBatchAsync(
            [MakeTick("1", Exchange.Binance), MakeTick("2", Exchange.Kraken)],
            CancellationToken.None);

        captured.Should().HaveCount(2);
        captured.Should().Contain(x => x.Exchange == "Binance");
        captured.Should().Contain(x => x.Exchange == "Kraken");
    }

    [Fact]
    public async Task ProcessBatch_RepositoryThrows_ExceptionPropagates()
    {
        _repository.InsertBatchAsync(Arg.Any<IReadOnlyList<Tick>>(), Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("DB error"));

        var act = async () => await CreateService().ProcessBatchAsync([MakeTick()], CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
