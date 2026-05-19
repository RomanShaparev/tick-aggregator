using Testcontainers.RabbitMq;
using Xunit;

namespace TickAggregator.EndToEndTests.Infrastructure;

public sealed class RabbitMqFixture
{
    private readonly RabbitMqContainer _container = new RabbitMqBuilder()
        .WithImage("rabbitmq:3-alpine")
        .WithUsername("guest")
        .WithPassword("guest")
        .Build();

    public string Hostname => _container.Hostname;
    public ushort Port => _container.GetMappedPublicPort(5672);
    public string Username => "guest";
    public string Password => "guest";

    public async Task StartAsync()
    {
        await _container.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }
}
