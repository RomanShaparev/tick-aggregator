using MassTransit;

namespace TickAggregator.Infrastructure.Messaging;

public sealed class DlqProducer : IDlqProducer
{
    private readonly IBus _bus;

    public DlqProducer(IBus bus) => _bus = bus;

    public Task SendAsync(InvalidMessage message, CancellationToken ct)
        => _bus.Publish(message, ct);
}
