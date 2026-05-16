using Microsoft.Extensions.Logging;
using TickAggregator.Application.Interfaces;
using TickAggregator.Application.Messages;
using TickAggregator.Domain.Entities;
using TickAggregator.Domain.Interfaces;

namespace TickAggregator.Application.Services;

public sealed class TickProcessingService
{
    private readonly IEnumerable<IExchangeParser> _parsers;
    private readonly IDeduplicationService _deduplication;
    private readonly ITickRepository _repository;
    private readonly ITickCounter _counter;
    private readonly ILogger<TickProcessingService> _logger;

    public TickProcessingService(
        IEnumerable<IExchangeParser> parsers,
        IDeduplicationService deduplication,
        ITickRepository repository,
        ITickCounter counter,
        ILogger<TickProcessingService> logger)
    {
        _parsers = parsers;
        _deduplication = deduplication;
        _repository = repository;
        _counter = counter;
        _logger = logger;
    }

    // Returns number of unique ticks saved. Throws on DB error so MassTransit can nack the batch.
    public async Task<int> ProcessBatchAsync(IReadOnlyList<RawTickMessage> messages, CancellationToken ct)
    {
        var ticks = new List<Tick>(messages.Count);

        foreach (var msg in messages)
        {
            var parser = _parsers.FirstOrDefault(p => p.ExchangeName == msg.Exchange);
            if (parser is null)
            {
                _logger.LogWarning("No parser for exchange {Exchange}", msg.Exchange);
                continue;
            }

            try
            {
                foreach (var tick in parser.Parse(msg.Payload))
                {
                    if (!_deduplication.IsDuplicate(tick.Exchange, tick.TradeId))
                        ticks.Add(tick);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse message from {Exchange}", msg.Exchange);
            }
        }

        if (ticks.Count == 0)
            return 0;

        await _repository.InsertBatchAsync(ticks, ct);

        foreach (var tick in ticks)
            _counter.Increment(tick.Exchange);

        _logger.LogDebug("Saved {Saved}/{Total} ticks ({Deduped} deduped)",
            ticks.Count, messages.Count, messages.Count - ticks.Count);

        return ticks.Count;
    }
}
