using FluentAssertions;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TickAggregator.Application.Metrics;
using TickAggregator.Application.Services;
using TickAggregator.Domain.Entities;
using TickAggregator.Domain.Enums;
using TickAggregator.Domain.Interfaces;
using TickAggregator.Infrastructure.Database;
using TickAggregator.Infrastructure.Deduplication;
using TickAggregator.Infrastructure.Messaging;
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
        var services = new ServiceCollection()
            .AddLogging(b => b.SetMinimumLevel(LogLevel.Warning))
            .AddDbContextFactory<TickDbContext>(options =>
                options.UseNpgsql(_postgres.ConnectionString))
            .AddSingleton<ITickRepository, TickRepository>()
            .AddSingleton<IDeduplicationService, InMemoryDeduplicationService>()
            .AddSingleton<TickMetrics>()
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

                cfg.ReceiveEndpoint($"ticks-test-{Guid.NewGuid():N}", e =>
                {
                    e.Durable = true;
                    e.AutoDelete = true;
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

    private static Tick MakeTick(string tradeId, Exchange exchange = Exchange.Binance) => new()
    {
        TradeId = tradeId,
        Exchange = exchange,
        Ticker = "BTCUSDT",
        Price = 50000m,
        Volume = 0.001m,
        Timestamp = DateTimeOffset.UtcNow,
        ReceivedAt = DateTimeOffset.UtcNow,
    };

    [Fact]
    public async Task FullPipeline_Tick_SavedToDatabase()
    {
        await _postgres.ClearTicksAsync();
        var publisher = _provider!.GetRequiredService<IBus>();

        await publisher.Publish(MakeTick("t-1"));

        var count = await WaitForCountAsync(1, TimeSpan.FromSeconds(15));
        count.Should().Be(1);
    }

    [Fact]
    public async Task FullPipeline_DuplicateTick_SavedOnlyOnce()
    {
        await _postgres.ClearTicksAsync();
        var publisher = _provider!.GetRequiredService<IBus>();

        var tick = MakeTick("dup-1");
        await publisher.Publish(tick);
        await publisher.Publish(tick);

        var count = await WaitForCountAsync(1, TimeSpan.FromSeconds(15));
        count.Should().Be(1);
    }

    [Fact]
    public async Task FullPipeline_ThreeExchanges_AllSaved()
    {
        await _postgres.ClearTicksAsync();
        var publisher = _provider!.GetRequiredService<IBus>();

        await publisher.Publish(MakeTick("e-1", Exchange.Binance));
        await publisher.Publish(MakeTick("e-2", Exchange.Kraken));
        await publisher.Publish(MakeTick("e-3", Exchange.Bybit));

        var count = await WaitForCountAsync(3, TimeSpan.FromSeconds(15));
        count.Should().Be(3);
    }
}
