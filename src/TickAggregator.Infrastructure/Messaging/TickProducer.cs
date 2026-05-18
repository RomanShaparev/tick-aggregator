using MassTransit;
using TickAggregator.Application.Interfaces;
using TickAggregator.Domain.Entities;

namespace TickAggregator.Infrastructure.Messaging;

public sealed class TickProducer : ITickProducer
{
    private readonly IBus _bus;

    public TickProducer(IBus bus) => _bus = bus;

    public Task PublishAsync(Tick tick, CancellationToken ct)
        => _bus.Publish(tick, ct);
}
