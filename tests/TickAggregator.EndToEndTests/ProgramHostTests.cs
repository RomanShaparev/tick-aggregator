using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using TickAggregator.Domain.Entities;
using TickAggregator.Domain.Enums;
using TickAggregator.Domain.Interfaces;
using TickAggregator.EndToEndTests.Infrastructure;
using TickAggregator.Infrastructure.Database;
using TickAggregator.Worker;
using Xunit;

namespace TickAggregator.EndToEndTests;

public sealed class ProgramHostTests : IAsyncLifetime
{
    private readonly RabbitMqFixture _rabbitMq = new();
    private readonly PostgresFixture _postgres = new();
    private readonly string _queueSuffix = Guid.NewGuid().ToString("N");
    private IHost? _host;
    private CancellationTokenSource? _cts;

    public async Task InitializeAsync() =>
        await Task.WhenAll(_rabbitMq.InitializeAsync(), _postgres.InitializeAsync());

    public async Task DisposeAsync()
    {
        _cts?.Cancel();
        if (_host is not null)
        {
            await _host.StopAsync(TimeSpan.FromSeconds(5));
            _host.Dispose();
        }
        _cts?.Dispose();
        await _postgres.DisposeAsync();
        await _rabbitMq.DisposeAsync();
    }

    private IHost BuildHost(IReadOnlyList<Tick> ticks)
    {
        var config = new Dictionary<string, string?>
        {
            ["RabbitMq:Host"] = _rabbitMq.Hostname,
            ["RabbitMq:Port"] = _rabbitMq.Port.ToString(),
            ["RabbitMq:VirtualHost"] = "/",
            ["RabbitMq:Username"] = _rabbitMq.Username,
            ["RabbitMq:Password"] = _rabbitMq.Password,
            ["RabbitMq:QueueName"] = $"raw-ticks-e2e-{_queueSuffix}",
            ["RabbitMq:BatchMessageLimit"] = "1",
            ["RabbitMq:BatchTimeLimitMs"] = "100",
            ["RabbitMq:PrefetchCount"] = "10",
            ["Database:ConnectionString"] = _postgres.ConnectionString,
            ["Deduplication:TtlSeconds"] = "5",
            ["DataSources:0:Name"] = "Binance",
            ["DataSources:0:Url"] = "ws://localhost:1/ws/binance",
        };

        return HostBuilderFactory.Create([])
            .ConfigureAppConfiguration(b => b.AddInMemoryCollection(config))
            .ConfigureServices(services =>
            {
                services.RemoveAll<IExchangeDataSource>();
                services.AddSingleton<IExchangeDataSource>(new FakeExchangeDataSource(ticks));
            })
            .Build();
    }

    private async Task StartHostAsync(IReadOnlyList<Tick> ticks)
    {
        _host = BuildHost(ticks);
        _cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        _ = _host.RunAsync(_cts.Token);
        await Task.Delay(500); // дать хосту время стартовать
    }

    private async Task<int> WaitForCountAsync(int expected, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        int count;
        do
        {
            count = await _postgres.CountTicksAsync();
            if (count >= expected) return count;
            await Task.Delay(150);
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
    };

    [Fact]
    public async Task Host_SingleTick_SavedToDatabase()
    {
        await _postgres.ClearTicksAsync();
        await StartHostAsync([MakeTick("e2e-1")]);

        var count = await WaitForCountAsync(1, TimeSpan.FromSeconds(30));
        count.Should().Be(1);
    }

    [Fact]
    public async Task Host_DuplicateTick_SavedOnlyOnce()
    {
        await _postgres.ClearTicksAsync();
        var tick = MakeTick("e2e-dup");
        await StartHostAsync([tick, tick]);

        var count = await WaitForCountAsync(1, TimeSpan.FromSeconds(30));
        count.Should().Be(1);
    }

    [Fact]
    public async Task Host_MultipleExchanges_AllSaved()
    {
        await _postgres.ClearTicksAsync();
        await StartHostAsync([
            MakeTick("e2e-bin", Exchange.Binance),
            MakeTick("e2e-kra", Exchange.Kraken),
            MakeTick("e2e-bby", Exchange.Bybit),
        ]);

        var count = await WaitForCountAsync(3, TimeSpan.FromSeconds(30));
        count.Should().Be(3);
    }
}
