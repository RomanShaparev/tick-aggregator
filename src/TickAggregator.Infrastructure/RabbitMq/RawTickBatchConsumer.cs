using MassTransit;
using Microsoft.Extensions.Logging;
using TickAggregator.Application.Messages;
using TickAggregator.Application.Services;

namespace TickAggregator.Infrastructure.RabbitMq;

public sealed class RawTickBatchConsumer : IConsumer<Batch<RawTickMessage>>
{
    private readonly TickProcessingService _processor;
    private readonly ILogger<RawTickBatchConsumer> _logger;

    public RawTickBatchConsumer(TickProcessingService processor, ILogger<RawTickBatchConsumer> logger)
    {
        _processor = processor;
        _logger = logger;
    }

    // MassTransit acks весь батч если метод завершился без исключения.
    // При исключении — nack, RabbitMQ вернёт сообщения в очередь / dead-letter.
    public async Task Consume(ConsumeContext<Batch<RawTickMessage>> context)
    {
        var messages = context.Message
            .Select(m => m.Message)
            .ToList();

        _logger.LogDebug("Consuming batch of {Count} messages", messages.Count);

        await _processor.ProcessBatchAsync(messages, context.CancellationToken);
    }
}
