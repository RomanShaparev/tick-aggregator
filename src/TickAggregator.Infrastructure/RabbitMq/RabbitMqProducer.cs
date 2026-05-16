using MassTransit;
using TickAggregator.Application.Interfaces;
using TickAggregator.Application.Messages;

namespace TickAggregator.Infrastructure.RabbitMq;

public sealed class RabbitMqProducer : IMessageProducer
{
    private readonly IBus _bus;

    public RabbitMqProducer(IBus bus) => _bus = bus;

    public Task PublishAsync(RawTickMessage message, CancellationToken ct)
        => _bus.Publish(message, ct);
}
