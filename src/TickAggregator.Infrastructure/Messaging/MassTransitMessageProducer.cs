using MassTransit;
using TickAggregator.Application.Interfaces;
using TickAggregator.Domain.Entities;

namespace TickAggregator.Infrastructure.Messaging;

public sealed class MassTransitMessageProducer : IMessageProducer
{
    private readonly IBus _bus;

    public MassTransitMessageProducer(IBus bus) => _bus = bus;

    public Task PublishAsync(Tick tick, CancellationToken ct)
        => _bus.Publish(tick, ct);
}
