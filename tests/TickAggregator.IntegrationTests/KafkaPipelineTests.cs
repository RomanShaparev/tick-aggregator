using FluentAssertions;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using TickAggregator.Application.Metrics;
using TickAggregator.Application.Services;
using TickAggregator.Domain.Entities;
using TickAggregator.Domain.Enums;
using TickAggregator.Domain.Interfaces;
using TickAggregator.Infrastructure.Caching;
using TickAggregator.Infrastructure.Deduplication;
using TickAggregator.Infrastructure.Messaging;
using Xunit;

namespace TickAggregator.IntegrationTests;

public sealed class MessageQueueTests
{
    private static ServiceProvider BuildProvider(ITickRepository repository)
    {
        return new ServiceCollection()
            .AddSingleton(repository)
            .AddMemoryCache()
            .AddSingleton<ICache, InMemoryCache>()
            .AddSingleton<IDeduplicationService, DeduplicationService>()
            .AddSingleton(new TickMetrics())
            .AddSingleton<TickProcessingService>()
            .AddSingleton(NullLogger<TickBatchConsumer>.Instance)
            .AddSingleton(NullLogger<TickProcessingService>.Instance)
            .AddMassTransitTestHarness(x =>
            {
                x.AddConsumer<TickBatchConsumer>(c =>
                    c.Options<BatchOptions>(o => o
                        .SetMessageLimit(10)
                        .SetTimeLimit(TimeSpan.FromMilliseconds(200))
                        .SetConcurrencyLimit(1)));
            })
            .BuildServiceProvider(true);
    }

    private static Tick MakeTick(string tradeId, Exchange exchange = Exchange.Binance) => new()
    {
        TradeId = tradeId,
        Exchange = exchange,
        Ticker = "BTCUSDT",
        Price = 50000m,
        Volume = 0.001m,
        Timestamp = DateTimeOffset.UtcNow,
    };

    [Fact]
    public async Task Published_Tick_IsConsumedAndSaved()
    {
        var repository = Substitute.For<ITickRepository>();
        await using var provider = BuildProvider(repository);

        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        await harness.Bus.Publish(MakeTick("t-100"));

        Assert.True(await harness.Consumed.Any<Tick>());

        await repository.Received(1).InsertBatchAsync(
            Arg.Is<IReadOnlyList<Tick>>(list => list.Count == 1 && list[0].TradeId == "t-100"),
            Arg.Any<CancellationToken>());

        await harness.Stop();
    }

    [Fact]
    public async Task Published_DuplicateTicks_SavedOnlyOnce()
    {
        var repository = Substitute.For<ITickRepository>();
        await using var provider = BuildProvider(repository);

        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        await harness.Bus.Publish(MakeTick("dup-42"));
        await harness.Bus.Publish(MakeTick("dup-42"));

        Assert.True(await harness.Consumed.Any<Tick>());
        await Task.Delay(300);

        await repository.Received(1).InsertBatchAsync(
            Arg.Is<IReadOnlyList<Tick>>(list => list.Count == 1),
            Arg.Any<CancellationToken>());

        await harness.Stop();
    }

    [Fact]
    public async Task Published_MultipleExchanges_AllSaved()
    {
        var repository = Substitute.For<ITickRepository>();
        await using var provider = BuildProvider(repository);

        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        await harness.Bus.Publish(MakeTick("e-1", Exchange.Binance));
        await harness.Bus.Publish(MakeTick("e-2", Exchange.Kraken));

        Assert.True(await harness.Consumed.Any<Tick>());
        await Task.Delay(300);

        await repository.Received().InsertBatchAsync(
            Arg.Is<IReadOnlyList<Tick>>(list => list.Count >= 1),
            Arg.Any<CancellationToken>());

        await harness.Stop();
    }
}
