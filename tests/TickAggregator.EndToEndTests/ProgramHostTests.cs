using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using TickAggregator.Domain.Entities;
using TickAggregator.Domain.Enums;
using TickAggregator.EndToEndTests.Infrastructure;
using TickAggregator.Worker;
using Xunit;

namespace TickAggregator.EndToEndTests;

public sealed class ProgramHostTests : IClassFixture<TestEnvironmentFixture>, IAsyncLifetime
{
    private readonly TestEnvironmentFixture _env;

    public ProgramHostTests(TestEnvironmentFixture env) => _env = env;

    public Task InitializeAsync() => _env.ClearTicksAsync();

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    private Tick MakeTick(Exchange exchange, string tradeId, string ticker = "BTCUSDT",
        decimal price = 50000m, decimal volume = 0.001m)
        => new()
        {
            TradeId = tradeId,
            Exchange = exchange,
            Ticker = ticker,
            Price = price,
            Volume = volume,
            Timestamp = new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero),
        };

    private async Task<ExchangeWebSocketServerFixture> CreateAndStartExchangeAsync(params Tick[] ticks)
    {
        var server = new ExchangeWebSocketServerFixture(ticks);
        await server.StartAsync();
        return server;
    }

    private async Task<IHost> CreateAndStartHostAsync(params (Exchange exchange, Uri url)[] sources)
    {
        var config = new Dictionary<string, string?>
        {
            ["RabbitMq:Host"] = _env.RabbitMq.Hostname,
            ["RabbitMq:Port"] = _env.RabbitMq.Port.ToString(),
            ["RabbitMq:Username"] = _env.RabbitMq.Username,
            ["RabbitMq:Password"] = _env.RabbitMq.Password,
            ["RabbitMq:BatchMessageLimit"] = "1",
            ["RabbitMq:BatchTimeLimitMs"] = "100",
            ["Database:ConnectionString"] = _env.Postgres.ConnectionString,
        };

        for (var i = 0; i < sources.Length; i++)
        {
            config[$"DataSources:{i}:Name"] = sources[i].exchange.ToString();
            config[$"DataSources:{i}:Url"] = sources[i].url.ToString();
        }

        var host = HostBuilderFactory.Create([])
            .ConfigureAppConfiguration(b => b.AddInMemoryCollection(config))
            .Build();

        await host.StartAsync();

        return host;
    }

    private async Task<int> WaitForCountAsync(int expected, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        int count;
        do
        {
            count = await _env.CountTicksAsync();
            if (count >= expected) return count;
            await Task.Delay(100);
        } while (DateTime.UtcNow < deadline);

        return count;
    }

    [Fact]
    public async Task Host_SingleTick_SavedToDatabase()
    {
        var tick = MakeTick(Exchange.Binance, "1");
        await using var exchange = await CreateAndStartExchangeAsync(tick);

        using var host = await CreateAndStartHostAsync((Exchange.Binance, exchange.Uri));

        var savedTicksCount = await WaitForCountAsync(1, TimeSpan.FromSeconds(5));
        savedTicksCount.Should().Be(1);

        var savedTicks = await _env.GetTicksAsync();
        savedTicks.Should().ContainSingle().Which.Should().BeEquivalentTo(tick);
    }

    [Fact]
    public async Task Host_DuplicateTick_SavedOnlyOnce()
    {
        var tick = MakeTick(Exchange.Binance, "1");
        await using var exchange = await CreateAndStartExchangeAsync(tick, tick);

        using var host = await CreateAndStartHostAsync((Exchange.Binance, exchange.Uri));

        var savedTicksCount = await WaitForCountAsync(1, TimeSpan.FromSeconds(5));
        savedTicksCount.Should().Be(1);

        var savedTicks = await _env.GetTicksAsync();
        savedTicks.Should().ContainSingle().Which.Should().BeEquivalentTo(tick);
    }

    [Fact]
    public async Task Host_MultipleExchanges_AllSaved()
    {
        var binanceTick = MakeTick(Exchange.Binance, "1");
        var bybitTick = MakeTick(Exchange.Bybit, "1");
        var krakenTick = MakeTick(Exchange.Kraken, "1");

        await using var binance = await CreateAndStartExchangeAsync(binanceTick);
        await using var bybit = await CreateAndStartExchangeAsync(bybitTick);
        await using var kraken = await CreateAndStartExchangeAsync(krakenTick);

        using var host = await CreateAndStartHostAsync(
            (Exchange.Binance, binance.Uri),
            (Exchange.Bybit, bybit.Uri),
            (Exchange.Kraken, kraken.Uri));

        var savedTicksCount = await WaitForCountAsync(3, TimeSpan.FromSeconds(5));
        savedTicksCount.Should().Be(3);

        var savedTicks = await _env.GetTicksAsync();
        savedTicks.Should().BeEquivalentTo([binanceTick, bybitTick, krakenTick]);
    }
}