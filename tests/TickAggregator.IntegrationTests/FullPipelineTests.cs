using FluentAssertions;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TickAggregator.Application.Interfaces;
using TickAggregator.Application.Messages;
using TickAggregator.Application.Services;
using TickAggregator.Domain.Interfaces;
using TickAggregator.Infrastructure.Configuration;
using TickAggregator.Infrastructure.Database;
using TickAggregator.Infrastructure.Deduplication;
using TickAggregator.Infrastructure.Parsers;
using TickAggregator.Infrastructure.RabbitMq;
using TickAggregator.IntegrationTests.Infrastructure;
using Xunit;

namespace TickAggregator.IntegrationTests;

public sealed class FullPipelineTests : IAsyncLifetime
{
    private readonly RabbitMqFixture _rabbitMq = new();
    private readonly PostgresFixture _postgres = new();
    private ServiceProvider? _provider;
    private IBusControl? _bus;

    public async Task InitializeAsync()
    {
        await Task.WhenAll(_rabbitMq.InitializeAsync(), _postgres.InitializeAsync());
        _provider = BuildProvider(batchMessageLimit: 1, batchTimeLimitMs: 100);
        _bus = _provider.GetRequiredService<IBusControl>();
        await _bus.StartAsync(CancellationToken.None);
    }

    public async Task DisposeAsync()
    {
        if (_bus is not null) await _bus.StopAsync(CancellationToken.None);
        if (_provider is not null) await _provider.DisposeAsync();
        await _postgres.DisposeAsync();
        await _rabbitMq.DisposeAsync();
    }

    private ServiceProvider BuildProvider(int batchMessageLimit, int batchTimeLimitMs)
    {
        var dbOpts = Options.Create(new DatabaseOptions { ConnectionString = _postgres.ConnectionString });
        var services = new ServiceCollection()
            .AddLogging(b => b.SetMinimumLevel(LogLevel.Warning))
            .AddSingleton<ITickRepository>(new TickRepository(dbOpts))
            .AddSingleton<IDeduplicationService, InMemoryDeduplicationService>()
            .AddSingleton<ITickCounter, TickCounterService>()
            .AddSingleton<IExchangeParser, BinanceParser>()
            .AddSingleton<IExchangeParser, KrakenParser>()
            .AddSingleton<IExchangeParser, BybitParser>()
            .AddSingleton<TickProcessingService>();

        services.AddMassTransit(x =>
        {
            x.AddConsumer<RawTickBatchConsumer>(c =>
                c.Options<BatchOptions>(o => o
                    .SetMessageLimit(batchMessageLimit)
                    .SetTimeLimit(TimeSpan.FromMilliseconds(batchTimeLimitMs))
                    .SetConcurrencyLimit(1)));

            x.UsingRabbitMq((ctx, cfg) =>
            {
                cfg.Host(_rabbitMq.Hostname, _rabbitMq.Port, "/", h =>
                {
                    h.Username(_rabbitMq.Username);
                    h.Password(_rabbitMq.Password);
                });

                cfg.ReceiveEndpoint($"raw-ticks-test-{Guid.NewGuid():N}", e =>
                {
                    e.Durable = true;
                    e.AutoDelete = true; // auto-delete in tests
                    e.PrefetchCount = 10;
                    e.ConfigureConsumer<RawTickBatchConsumer>(ctx);
                });
            });
        });

        return services.BuildServiceProvider(true);
    }

    private async Task<int> WaitForCountAsync(int expected, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        int count;
        do
        {
            count = await _postgres.CountTicksAsync();
            if (count >= expected) return count;
            await Task.Delay(100);
        }
        while (DateTime.UtcNow < deadline);
        return count;
    }

    [Fact]
    public async Task FullPipeline_BinanceTick_SavedToDatabase()
    {
        await _postgres.ClearTicksAsync();
        var publisher = _provider!.GetRequiredService<IBus>();

        var rawTick = """{"e":"trade","t":99991,"s":"BTCUSDT","p":"50000.00","q":"0.001","T":1716000000000}""";
        await publisher.Publish(new RawTickMessage { Exchange = "Binance", Payload = rawTick });

        var count = await WaitForCountAsync(1, TimeSpan.FromSeconds(15));
        count.Should().Be(1);
    }

    [Fact]
    public async Task FullPipeline_DuplicateTick_SavedOnlyOnce()
    {
        await _postgres.ClearTicksAsync();
        var publisher = _provider!.GetRequiredService<IBus>();

        var rawTick = """{"e":"trade","t":77777,"s":"ETHUSDT","p":"3000.00","q":"0.1","T":1716000000000}""";
        var message = new RawTickMessage { Exchange = "Binance", Payload = rawTick };

        await publisher.Publish(message);
        await publisher.Publish(message);

        var count = await WaitForCountAsync(1, TimeSpan.FromSeconds(15));
        count.Should().Be(1);
    }

    [Fact]
    public async Task FullPipeline_ThreeExchanges_AllSaved()
    {
        await _postgres.ClearTicksAsync();
        var publisher = _provider!.GetRequiredService<IBus>();

        await publisher.Publish(new RawTickMessage
        {
            Exchange = "Binance",
            Payload = """{"e":"trade","t":1001,"s":"BTCUSDT","p":"50000.00","q":"0.001","T":1716000000000}"""
        });
        await publisher.Publish(new RawTickMessage
        {
            Exchange = "Kraken",
            Payload = """{"channel":"trade","data":[{"trade_id":2001,"symbol":"BTC/USD","side":"buy","price":50001.0,"qty":0.001,"timestamp":"2024-05-18T10:00:00.000000Z"}]}"""
        });
        await publisher.Publish(new RawTickMessage
        {
            Exchange = "Bybit",
            Payload = """{"topic":"publicTrade.BTCUSDT","ts":1716000000000,"data":[{"i":"abc3001xyz","T":1716000000000,"p":"50002.00","v":"0.001","S":"Buy","s":"BTCUSDT"}]}"""
        });

        var count = await WaitForCountAsync(3, TimeSpan.FromSeconds(15));
        count.Should().Be(3);
    }
}
