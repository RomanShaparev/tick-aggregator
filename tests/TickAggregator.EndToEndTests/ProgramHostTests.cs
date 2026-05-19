using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using TickAggregator.Domain.Entities;
using TickAggregator.Domain.Enums;
using TickAggregator.Domain.Interfaces;
using TickAggregator.EndToEndTests.Infrastructure;
using TickAggregator.Worker;
using Xunit;

namespace TickAggregator.EndToEndTests;

public sealed class ProgramHostTests : IAsyncLifetime
{
    private readonly RabbitMqFixture _rabbitMq = new();
    private readonly PostgresFixture _postgres = new();
    private IHost? _host;

    public async Task InitializeAsync()
    {
        await Task.WhenAll(_rabbitMq.StartAsync(), _postgres.StartAsync());
    }

    public async Task DisposeAsync()
    {
        if (_host is not null)
        {
            await _host.StopAsync(TimeSpan.FromSeconds(5));
            _host.Dispose();
        }
        await _postgres.DisposeAsync();
        await _rabbitMq.DisposeAsync();
    }

    private IHost BuildHost(IReadOnlyList<Tick> ticks)
    {
        var config = new Dictionary<string, string?>
        {
            ["RabbitMq:Host"] = _rabbitMq.Hostname,
            ["RabbitMq:Port"] = _rabbitMq.Port.ToString(),
            ["RabbitMq:Username"] = _rabbitMq.Username,
            ["RabbitMq:Password"] = _rabbitMq.Password,
            ["RabbitMq:BatchMessageLimit"] = "1",
            ["RabbitMq:BatchTimeLimitMs"] = "100",
            ["RabbitMq:PrefetchCount"] = "10",
            ["Database:ConnectionString"] = _postgres.ConnectionString,
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
        await _host.StartAsync();
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

    private Tick MakeTick(string tradeId, Exchange exchange = Exchange.Binance) => new()
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
        await StartHostAsync([MakeTick("e2e-1")]);
    
        var count = await WaitForCountAsync(1, TimeSpan.FromSeconds(5));
        count.Should().Be(1);
    }

    [Fact]
    public async Task Host_DuplicateTick_SavedOnlyOnce()
    {
        var tick = MakeTick("e2e-dup");
        await StartHostAsync([tick, tick]);

        var count = await WaitForCountAsync(1, TimeSpan.FromSeconds(5));
        count.Should().Be(1);
    }

    [Fact]
    public async Task Host_MultipleExchanges_AllSaved()
    {
        await StartHostAsync([
            MakeTick("e2e-bin", Exchange.Binance),
            MakeTick("e2e-kra", Exchange.Kraken),
            MakeTick("e2e-bby", Exchange.Bybit),
        ]);

        var count = await WaitForCountAsync(3, TimeSpan.FromSeconds(5));
        count.Should().Be(3);
    }
}
