using MassTransit;
using Microsoft.Extensions.Logging;
using TickAggregator.Application.Services;
using TickAggregator.Domain.Entities;

namespace TickAggregator.Infrastructure.Messaging;

public sealed class RawTickBatchConsumer : IConsumer<Batch<Tick>>
{
    private readonly TickProcessingService _processor;
    private readonly ILogger<RawTickBatchConsumer> _logger;

    public RawTickBatchConsumer(TickProcessingService processor, ILogger<RawTickBatchConsumer> logger)
    {
        _processor = processor;
        _logger = logger;
    }
    
    public async Task Consume(ConsumeContext<Batch<Tick>> context)
    {
        var ticks = context.Message
            .Select(m => m.Message)
            .ToList();

        _logger.LogDebug("Consuming batch of {Count} ticks", ticks.Count);

        await _processor.ProcessBatchAsync(ticks, context.CancellationToken);
    }
}
