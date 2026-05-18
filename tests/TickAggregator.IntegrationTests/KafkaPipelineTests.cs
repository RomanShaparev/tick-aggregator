using FluentAssertions;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using TickAggregator.Application.Metrics;
using TickAggregator.Application.Services;
using TickAggregator.Domain.Entities;
using TickAggregator.Domain.Enums;
using TickAggregator.Domain.Interfaces;
using TickAggregator.Infrastructure.Caching;
using TickAggregator.Infrastructure.Configuration;
using TickAggregator.Infrastructure.Deduplication;
using TickAggregator.Infrastructure.Messaging;
using Xunit;

namespace TickAggregator.IntegrationTests;

public sealed class MessageQueueTests
{
    private static ServiceProvider BuildProvider(ITickRepository repository)
    {
        return new ServiceCollection()
            .AddLogging(b => b.SetMinimumLevel(LogLevel.Warning))
            .AddSingleton(repository)
            .AddMemoryCache()
            .AddSingleton<ICache, InMemoryCache>()
            .AddSingleton(Options.Create(new DeduplicationOptions { TtlSeconds = 300 }))
            .AddSingleton<IDeduplicationService, DeduplicationService>()
            .AddSingleton(new TickMetrics())
            .AddSingleton<TickProcessingService>()
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
        var consumerHarness = provider.GetRequiredService<IConsumerTestHarness<TickBatchConsumer>>();
        await harness.Start();

        await harness.Bus.Publish(MakeTick("t-100"));

        Assert.True(await consumerHarness.Consumed.Any<Batch<Tick>>());

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
        var consumerHarness = provider.GetRequiredService<IConsumerTestHarness<TickBatchConsumer>>();
        await harness.Start();

        await harness.Bus.Publish(MakeTick("dup-42"));
        await harness.Bus.Publish(MakeTick("dup-42"));

        Assert.True(await consumerHarness.Consumed.Any<Batch<Tick>>());

        await repository.Received(1).InsertBatchAsync(
            Arg.Is<IReadOnlyList<Tick>>(list => list.Count == 1),
            Arg.Any<CancellationToken>());

        await harness.Stop();
    }

    [Fact]
    public async Task Published_MultipleExchanges_AllSaved()
    {
        var allSavedTicks = new List<Tick>();
        var repository = Substitute.For<ITickRepository>();
        repository.InsertBatchAsync(Arg.Any<IReadOnlyList<Tick>>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask)
            .AndDoes(call => allSavedTicks.AddRange(call.Arg<IReadOnlyList<Tick>>()));

        await using var provider = BuildProvider(repository);
        var harness = provider.GetRequiredService<ITestHarness>();
        var consumerHarness = provider.GetRequiredService<IConsumerTestHarness<TickBatchConsumer>>();
        await harness.Start();

        await harness.Bus.Publish(MakeTick("e-1", Exchange.Binance));
        await harness.Bus.Publish(MakeTick("e-2", Exchange.Kraken));

        Assert.True(await consumerHarness.Consumed.Any<Batch<Tick>>());
        await Task.Delay(300);

        allSavedTicks.Should().HaveCountGreaterThanOrEqualTo(2);
        allSavedTicks.Should().Contain(t => t.TradeId == "e-1" && t.Exchange == Exchange.Binance);
        allSavedTicks.Should().Contain(t => t.TradeId == "e-2" && t.Exchange == Exchange.Kraken);

        await harness.Stop();
    }
}
