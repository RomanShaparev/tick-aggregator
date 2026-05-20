using Xunit;

namespace TickAggregator.EndToEndTests.Infrastructure;

public sealed class TestEnvironmentFixture : IAsyncLifetime
{
    private readonly RabbitMqFixture _rabbitMq = new();
    private readonly PostgresFixture _postgres = new();

    public string RabbitMqHostname => _rabbitMq.Hostname;
    public ushort RabbitMqPort => _rabbitMq.Port;
    public string RabbitMqUsername => _rabbitMq.Username;
    public string RabbitMqPassword => _rabbitMq.Password;
    public string PostgresConnectionString => _postgres.ConnectionString;

    public Task InitializeAsync()
        => Task.WhenAll(_rabbitMq.StartAsync(), _postgres.StartAsync());

    public async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
        await _rabbitMq.DisposeAsync();
    }

    public Task<int> CountTicksAsync() => _postgres.CountTicksAsync();

    public Task ClearTicksAsync() => _postgres.ClearTicksAsync();
}
