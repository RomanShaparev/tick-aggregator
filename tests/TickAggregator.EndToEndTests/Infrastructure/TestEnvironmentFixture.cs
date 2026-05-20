using TickAggregator.Domain.Entities;
using Xunit;

namespace TickAggregator.EndToEndTests.Infrastructure;

public sealed class TestEnvironmentFixture : IAsyncLifetime
{
    private readonly RabbitMqFixture _rabbitMq = new();
    private readonly PostgresFixture _postgres = new();

    public RabbitMqFixture RabbitMq => _rabbitMq;
    public PostgresFixture Postgres => _postgres;

    public Task InitializeAsync()
    {
        return Task.WhenAll(_rabbitMq.StartAsync(), _postgres.StartAsync());
    }

    public Task DisposeAsync()
    {
        return Task.WhenAll(_postgres.DisposeAsync(), _rabbitMq.DisposeAsync());
    }

    public Task<int> CountTicksAsync() => _postgres.CountTicksAsync();

    public Task<IReadOnlyList<Tick>> GetTicksAsync() => _postgres.GetTicksAsync();

    public Task ClearTicksAsync() => _postgres.ClearTicksAsync();
}
