using Testcontainers.RabbitMq;
using Xunit;

namespace TickAggregator.IntegrationTests.Infrastructure;

public sealed class RabbitMqFixture : IAsyncLifetime
{
    private readonly RabbitMqContainer _container = new RabbitMqBuilder()
        .WithImage("rabbitmq:3-alpine")
        .WithUsername("guest")
        .WithPassword("guest")
        .Build();

    public string Hostname => _container.Hostname;
    public ushort Port => (ushort)_container.GetMappedPublicPort(5672);
    public string Username => "guest";
    public string Password => "guest";

    public Task InitializeAsync() => _container.StartAsync();
    public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}
