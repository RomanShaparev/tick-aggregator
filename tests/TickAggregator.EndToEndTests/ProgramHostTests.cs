using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using TickAggregator.EndToEndTests.Infrastructure;
using TickAggregator.Worker;
using Xunit;

namespace TickAggregator.EndToEndTests;

public sealed class ProgramHostTests : IClassFixture<TestEnvironmentFixture>, IAsyncLifetime
{
    private readonly TestEnvironmentFixture _env;
    private IHost? _host;
    private readonly List<IAsyncDisposable> _servers = [];

    public ProgramHostTests(TestEnvironmentFixture env) => _env = env;

    public Task InitializeAsync() => _env.ClearTicksAsync();

    public async Task DisposeAsync()
    {
        if (_host is not null)
        {
            await _host.StopAsync(TimeSpan.FromSeconds(5));
            _host.Dispose();
        }
        foreach (var server in _servers)
            await server.DisposeAsync();
    }

    private async Task<ExchangeWebSocketServerFixture> CreateServerAsync(IReadOnlyList<string> messages)
    {
        var server = new ExchangeWebSocketServerFixture(messages);
        await server.StartAsync();
        _servers.Add(server);
        return server;
    }

    private IHost BuildHost(params (string exchange, Uri url)[] sources)
    {
        var config = new Dictionary<string, string?>
        {
            ["RabbitMq:Host"] = _env.RabbitMqHostname,
            ["RabbitMq:Port"] = _env.RabbitMqPort.ToString(),
            ["RabbitMq:Username"] = _env.RabbitMqUsername,
            ["RabbitMq:Password"] = _env.RabbitMqPassword,
            ["RabbitMq:BatchMessageLimit"] = "1",
            ["RabbitMq:BatchTimeLimitMs"] = "100",
            ["RabbitMq:PrefetchCount"] = "10",
            ["Database:ConnectionString"] = _env.PostgresConnectionString,
        };

        for (var i = 0; i < sources.Length; i++)
        {
            config[$"DataSources:{i}:Name"] = sources[i].exchange;
            config[$"DataSources:{i}:Url"] = sources[i].url.ToString();
        }

        return HostBuilderFactory.Create([])
            .ConfigureAppConfiguration(b => b.AddInMemoryCollection(config))
            .Build();
    }

    private async Task StartHostAsync(params (string exchange, Uri url)[] sources)
    {
        _host = BuildHost(sources);
        await _host.StartAsync();
    }

    private async Task<int> WaitForCountAsync(int expected, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        int count;
        do
        {
            count = await _env.CountTicksAsync();
            if (count >= expected) return count;
            await Task.Delay(150);
        }
        while (DateTime.UtcNow < deadline);
        return count;
    }

    [Fact]
    public async Task Host_SingleTick_SavedToDatabase()
    {
        var binance = await CreateServerAsync([ExchangeWebSocketServerFixture.Binance(1)]);

        await StartHostAsync(("Binance", binance.Uri));

        (await WaitForCountAsync(1, TimeSpan.FromSeconds(5))).Should().Be(1);
    }

    [Fact]
    public async Task Host_DuplicateTick_SavedOnlyOnce()
    {
        var msg = ExchangeWebSocketServerFixture.Binance(1);
        var binance = await CreateServerAsync([msg, msg]);

        await StartHostAsync(("Binance", binance.Uri));

        (await WaitForCountAsync(1, TimeSpan.FromSeconds(5))).Should().Be(1);
    }

    [Fact]
    public async Task Host_MultipleExchanges_AllSaved()
    {
        var binance = await CreateServerAsync([ExchangeWebSocketServerFixture.Binance(1)]);
        var bybit   = await CreateServerAsync([ExchangeWebSocketServerFixture.Bybit("1")]);
        var kraken  = await CreateServerAsync([ExchangeWebSocketServerFixture.Kraken(1)]);

        await StartHostAsync(
            ("Binance", binance.Uri),
            ("Bybit",   bybit.Uri),
            ("Kraken",  kraken.Uri));

        (await WaitForCountAsync(3, TimeSpan.FromSeconds(5))).Should().Be(3);
    }
}
