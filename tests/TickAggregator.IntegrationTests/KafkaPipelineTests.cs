using FluentAssertions;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using TickAggregator.Application.Interfaces;
using TickAggregator.Application.Messages;
using TickAggregator.Application.Services;
using TickAggregator.Domain.Entities;
using TickAggregator.Domain.Interfaces;
using TickAggregator.Infrastructure.Deduplication;
using TickAggregator.Infrastructure.Parsers;
using TickAggregator.Infrastructure.RabbitMq;
using Xunit;

namespace TickAggregator.IntegrationTests;

// Tests use MassTransit in-memory test harness — no container required.
public sealed class MessageQueueTests
{
    private static ServiceProvider BuildProvider(ITickRepository repository)
    {
        return new ServiceCollection()
            .AddSingleton(repository)
            .AddSingleton<IDeduplicationService, InMemoryDeduplicationService>()
            .AddSingleton<ITickCounter, TickCounterService>()
            .AddSingleton<IExchangeParser, BinanceParser>()
            .AddSingleton<IExchangeParser, KrakenParser>()
            .AddSingleton<IExchangeParser, BybitParser>()
            .AddSingleton<TickProcessingService>()
            .AddSingleton(NullLogger<RawTickBatchConsumer>.Instance)
            .AddSingleton(NullLogger<TickProcessingService>.Instance)
            .AddMassTransitTestHarness(x =>
            {
                x.AddConsumer<RawTickBatchConsumer>(c =>
                    c.Options<BatchOptions>(o => o
                        .SetMessageLimit(10)
                        .SetTimeLimit(TimeSpan.FromMilliseconds(200))
                        .SetConcurrencyLimit(1)));
            })
            .BuildServiceProvider(true);
    }

    [Fact]
    public async Task Published_BinanceTick_IsConsumedAndSaved()
    {
        var repository = Substitute.For<ITickRepository>();
        await using var provider = BuildProvider(repository);

        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        var rawTick = """{"e":"trade","t":12345,"s":"BTCUSDT","p":"50000.00","q":"0.001","T":1716000000000}""";
        await harness.Bus.Publish(new RawTickMessage { Exchange = "Binance", Payload = rawTick });

        Assert.True(await harness.Consumed.Any<RawTickMessage>());

        await repository.Received(1).InsertBatchAsync(
            Arg.Is<IReadOnlyList<Tick>>(list => list.Count == 1 && list[0].TradeId == "12345"),
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

        var rawTick = """{"e":"trade","t":77777,"s":"ETHUSDT","p":"3000.00","q":"0.1","T":1716000000000}""";
        var message = new RawTickMessage { Exchange = "Binance", Payload = rawTick };

        await harness.Bus.Publish(message);
        await harness.Bus.Publish(message);

        Assert.True(await harness.Consumed.Any<RawTickMessage>());
        await Task.Delay(300); // wait for batch TimeLimit to fire

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

        var binanceTick = """{"e":"trade","t":1,"s":"BTCUSDT","p":"50000.00","q":"0.001","T":1716000000000}""";
        var krakenTick = """{"channel":"trade","data":[{"trade_id":2,"symbol":"BTC/USD","side":"buy","price":50001.00,"qty":0.001,"timestamp":"2024-05-18T10:00:00.000000Z"}]}""";

        await harness.Bus.Publish(new RawTickMessage { Exchange = "Binance", Payload = binanceTick });
        await harness.Bus.Publish(new RawTickMessage { Exchange = "Kraken", Payload = krakenTick });

        Assert.True(await harness.Consumed.Any<RawTickMessage>());
        await Task.Delay(300);

        await repository.Received().InsertBatchAsync(
            Arg.Is<IReadOnlyList<Tick>>(list => list.Count >= 1),
            Arg.Any<CancellationToken>());

        await harness.Stop();
    }
}
