using Testcontainers.RabbitMq;

namespace TickAggregator.EndToEndTests.Infrastructure;

public sealed class RabbitMqFixture
{
    private readonly RabbitMqContainer _container;

    public RabbitMqFixture()
    {
        _container = new RabbitMqBuilder()
            .WithImage("rabbitmq:3-alpine")
            .WithUsername(Username)
            .WithPassword(Password)
            .Build();
    }

    public string Hostname => _container.Hostname;
    public ushort Port => _container.GetMappedPublicPort(5672);
    public string Username => "test-user";
    public string Password => "test-password";

    public async Task StartAsync()
    {
        await _container.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }
}
